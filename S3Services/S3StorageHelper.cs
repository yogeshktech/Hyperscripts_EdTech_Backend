using HyperDroid.CloudKit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using POPI_TRACKER_BACKEND.S3Services;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace CareerCracker.S3Services
{
    public static class S3StorageHelper
    {
        private static IConfiguration? _config;

        // Call this in Program.cs after builder.Configuration is ready
        public static void Initialize(IConfiguration configuration)
        {
            _config = configuration;
        }

        private static CloudStorage? CreateClient()
        {
            var s3Section = _config?.GetSection("S3");
            if (s3Section == null) return null;

            var bucket = s3Section["BucketName"];
            var accessKey = s3Section["AccessKey"];
            var secretKey = s3Section["SecretKey"];

            if (string.IsNullOrWhiteSpace(bucket) ||
                string.IsNullOrWhiteSpace(accessKey) ||
                string.IsNullOrWhiteSpace(secretKey))
                return null;

            var cfg = new CloudStorageConfig
            {
                BucketName = bucket,
                AccessKey = accessKey,
                SecretKey = secretKey,
                ServiceUrl = s3Section["ServiceUrl"],
                PublicBaseUrl = s3Section["PublicBaseUrl"],
                Region = s3Section["Region"],
                ForcePathStyle = s3Section.GetValue<bool>("ForcePathStyle")
            };

            return new CloudStorage(cfg);
        }

        private static bool LooksLikeConnectionFailure(Exception ex)
        {
            for (var cur = ex; cur != null; cur = cur.InnerException)
            {
                if (cur is SocketException se &&
                    (se.SocketErrorCode == SocketError.ConnectionRefused ||
                     se.SocketErrorCode == SocketError.HostNotFound ||
                     se.SocketErrorCode == SocketError.TimedOut))
                    return true;
            }

            return ex.Message.Contains("actively refused", StringComparison.OrdinalIgnoreCase)
                   || ex.Message.Contains("Connection refused", StringComparison.OrdinalIgnoreCase);
        }



        public static async Task<string?> UploadFileAsync(IFormFile file, string folderPrefix = "uploads")
        {
            if (file == null || file.Length == 0) return null;

            using var client = CreateClient();
            if (client == null) return null;

            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? "";
            var key = $"{folderPrefix.TrimEnd('/')}/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}{ext}";

            await using var stream = file.OpenReadStream();

            try
            {
                await client.UploadStreamAsync(
                    stream,
                    key,
                    file.FileName,
                    file.ContentType,
                    file.Length);

                // ✅ Build public HTTPS/CDN URL (never expose MinIO ServiceUrl / IP:9000 to clients)
                var s3 = _config?.GetSection("S3");

                string baseUrl = s3?["PublicBaseUrl"]?.TrimEnd('/') ?? "";
                string bucket = s3?["BucketName"] ?? "";
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return ToPublicUrl($"{bucket}/{key}");

                // Avoid duplicate bucket in URL when PublicBaseUrl already contains "/{bucket}"
                if (!string.IsNullOrWhiteSpace(bucket) &&
                    baseUrl.EndsWith("/" + bucket, StringComparison.OrdinalIgnoreCase))
                    return ToPublicUrl($"{baseUrl}/{key}");

                return ToPublicUrl($"{baseUrl}/{bucket}/{key}");
            }
            catch (Exception ex) when (LooksLikeConnectionFailure(ex))
            {
                throw;
            }
        }

        /// <summary>
        /// Rewrites MinIO/ServiceUrl (IP:9000 / http) URLs to the configured PublicBaseUrl (e.g. https://edtech.colaborazia.com/...).
        /// Use when returning stored image paths from the DB so live HTTPS sites do not get mixed-content / IP URLs.
        /// </summary>
        public static string? ToPublicUrl(string? pathOrUrl)
        {
            if (string.IsNullOrWhiteSpace(pathOrUrl))
                return pathOrUrl;

            var s = pathOrUrl.Trim();
            var s3 = _config?.GetSection("S3");
            var publicBase = s3?["PublicBaseUrl"]?.TrimEnd('/');
            var serviceUrl = s3?["ServiceUrl"]?.TrimEnd('/');
            var bucket = s3?["BucketName"]?.Trim();

            if (string.IsNullOrWhiteSpace(publicBase))
                return s;

            // Already on public base
            if (s.StartsWith(publicBase, StringComparison.OrdinalIgnoreCase))
                return s;

            // Relative key → public URL
            if (!s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                var key = s.TrimStart('/');
                if (!string.IsNullOrWhiteSpace(bucket) &&
                    publicBase.EndsWith("/" + bucket, StringComparison.OrdinalIgnoreCase))
                    return $"{publicBase}/{key}";

                if (!string.IsNullOrWhiteSpace(bucket) &&
                    key.StartsWith(bucket + "/", StringComparison.OrdinalIgnoreCase))
                    return $"{publicBase}/{key}";

                if (!string.IsNullOrWhiteSpace(bucket))
                    return $"{publicBase}/{bucket}/{key}";

                return $"{publicBase}/{key}";
            }

            // Replace ServiceUrl host (http or https) with PublicBaseUrl
            if (!string.IsNullOrWhiteSpace(serviceUrl) &&
                Uri.TryCreate(serviceUrl, UriKind.Absolute, out var serviceUri) &&
                Uri.TryCreate(s, UriKind.Absolute, out var storedUri))
            {
                var sameHost =
                    string.Equals(storedUri.Host, serviceUri.Host, StringComparison.OrdinalIgnoreCase) &&
                    storedUri.Port == serviceUri.Port;

                // Also match when DB has https://IP:9000 but ServiceUrl is http://IP:9000
                var sameHostIgnoreScheme =
                    string.Equals(storedUri.Host, serviceUri.Host, StringComparison.OrdinalIgnoreCase) &&
                    (storedUri.Port == serviceUri.Port ||
                     storedUri.Port == 9000 && serviceUri.Port == 9000);

                if (sameHost || sameHostIgnoreScheme)
                {
                    var path = storedUri.AbsolutePath.TrimStart('/');
                    return $"{publicBase}/{path}";
                }
            }

            // Fallback: known MinIO IP:9000 left in older rows
            const string legacyHost = "116.203.133.249:9000";
            if (s.Contains(legacyHost, StringComparison.OrdinalIgnoreCase))
            {
                var idx = s.IndexOf(legacyHost, StringComparison.OrdinalIgnoreCase);
                var after = s[(idx + legacyHost.Length)..].TrimStart('/');
                return $"{publicBase}/{after}";
            }

            return s;
        }


        public static async Task<bool> DeleteByPathAsync(string? pathOrUrl)
        {
            if (string.IsNullOrWhiteSpace(pathOrUrl)) return true;

            using var client = CreateClient();
            if (client == null) return true;

            var key = ExtractKeyFromUrl(pathOrUrl);
            if (string.IsNullOrWhiteSpace(key)) return true;

            return await client.DeleteFileAsync(key);
        }

        /// <summary>
        /// Removes legacy files under wwwroot (e.g. /uploads/blogs/...) or deletes the object when <paramref name="pathOrUrl"/> is an http(s) URL from S3/MinIO.
        /// </summary>
        public static async Task DeleteStoredMediaAsync(string? pathOrUrl)
        {
            if (string.IsNullOrWhiteSpace(pathOrUrl)) return;

            var s = pathOrUrl.Trim();
            if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                await DeleteByPathAsync(s);
                return;
            }

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", s.TrimStart('/', '\\'));
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }

        private static string? ExtractKeyFromUrl(string urlOrPath)
        {
            if (string.IsNullOrWhiteSpace(urlOrPath)) return null;

            var s3Section = _config?.GetSection("S3");
            var publicBase = s3Section?["PublicBaseUrl"];
            var serviceUrl = s3Section?["ServiceUrl"];
            var bucket = s3Section?["BucketName"];
            var forcePathStyle = s3Section?.GetValue<bool>("ForcePathStyle") ?? true;

            // Already a key
            if (!urlOrPath.StartsWith("http", StringComparison.OrdinalIgnoreCase) &&
                !urlOrPath.StartsWith("/"))
                return urlOrPath;

            // Match PublicBaseUrl
            if (!string.IsNullOrWhiteSpace(publicBase) &&
                urlOrPath.StartsWith(publicBase.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
            {
                var prefix = publicBase.TrimEnd('/') + "/";
                var remainder = urlOrPath.Substring(prefix.Length);
                if (!string.IsNullOrWhiteSpace(bucket) &&
                    remainder.StartsWith(bucket + "/", StringComparison.OrdinalIgnoreCase))
                {
                    return remainder.Substring(bucket.Length + 1);
                }
                return remainder;
            }

            // Match ServiceUrl (MinIO style) — http or https on same host:port
            if (!string.IsNullOrWhiteSpace(serviceUrl) &&
                Uri.TryCreate(serviceUrl, UriKind.Absolute, out var serviceUri) &&
                Uri.TryCreate(urlOrPath, UriKind.Absolute, out var storedUri) &&
                string.Equals(storedUri.Host, serviceUri.Host, StringComparison.OrdinalIgnoreCase) &&
                storedUri.Port == serviceUri.Port)
            {
                var remainder = storedUri.AbsolutePath.TrimStart('/');

                if (forcePathStyle && !string.IsNullOrWhiteSpace(bucket) &&
                    remainder.StartsWith(bucket + "/", StringComparison.OrdinalIgnoreCase))
                {
                    return remainder.Substring(bucket.Length + 1);
                }
                return remainder;
            }

            // Legacy MinIO IP URLs saved before PublicBaseUrl was set
            const string legacyHost = "116.203.133.249:9000";
            if (urlOrPath.Contains(legacyHost, StringComparison.OrdinalIgnoreCase))
            {
                var idx = urlOrPath.IndexOf(legacyHost, StringComparison.OrdinalIgnoreCase);
                var remainder = urlOrPath[(idx + legacyHost.Length)..].TrimStart('/');
                if (forcePathStyle && !string.IsNullOrWhiteSpace(bucket) &&
                    remainder.StartsWith(bucket + "/", StringComparison.OrdinalIgnoreCase))
                {
                    return remainder.Substring(bucket.Length + 1);
                }
                return remainder;
            }

            return null;
        }

        internal static async Task DeleteFileAsync(string oldImage)
        {
            await DeleteStoredMediaAsync(oldImage);
        }
    }
}