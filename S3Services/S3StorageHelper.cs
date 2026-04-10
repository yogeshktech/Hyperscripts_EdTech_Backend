using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using HyperDroid.CloudKit;
using POPI_TRACKER_BACKEND.S3Services;

namespace CareerCracker.S3Services
{
    public static class S3StorageHelper
    {
        private static CloudStorage? CreateClient()
        {
            var bucket = Environment.GetEnvironmentVariable("S3_BUCKET");
            var access = Environment.GetEnvironmentVariable("S3_ACCESS_KEY");
            var secret = Environment.GetEnvironmentVariable("S3_SECRET_KEY");
            var serviceUrl = Environment.GetEnvironmentVariable("S3_SERVICE_URL");
            var publicBase = Environment.GetEnvironmentVariable("S3_PUBLIC_BASE_URL");
            var forcePath = Environment.GetEnvironmentVariable("S3_FORCE_PATH_STYLE");

            if (string.IsNullOrWhiteSpace(bucket) || string.IsNullOrWhiteSpace(access) || string.IsNullOrWhiteSpace(secret))
                return null; // S3 not configured

            var cfg = new CloudStorageConfig
            {
                BucketName = bucket,
                AccessKey = access,
                SecretKey = secret,
                ServiceUrl = string.IsNullOrWhiteSpace(serviceUrl) ? null : serviceUrl,
                PublicBaseUrl = string.IsNullOrWhiteSpace(publicBase) ? null : publicBase,
                ForcePathStyle = string.Equals(forcePath, "true", StringComparison.OrdinalIgnoreCase)
            };

            return new CloudStorage(cfg);
        }

        public static async Task<string?> UploadFileAsync(IFormFile file, string folderPrefix = "uploads")
        {
            if (file == null || file.Length == 0) return null;

            using var client = CreateClient();
            if (client == null)
                return null; // not configured

            var ext = Path.GetExtension(file.FileName);
            var key = $"{folderPrefix.TrimEnd('/')}/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid()}{ext}";

            await using var stream = file.OpenReadStream();
            var result = await client.UploadStreamAsync(stream, key, file.FileName, file.ContentType, file.Length);
            return result.Url;
        }

        public static async Task<bool> DeleteByPathAsync(string? pathOrUrl)
        {
            if (string.IsNullOrWhiteSpace(pathOrUrl)) return true;

            // If path is local (starts with '/') try delete file locally as well
            try
            {
                if (pathOrUrl.StartsWith("/"))
                {
                    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", pathOrUrl.TrimStart('/'));
                    if (File.Exists(fullPath)) File.Delete(fullPath);
                }
            }
            catch
            {
                // ignore local delete errors
            }

            using var client = CreateClient();
            if (client == null) return true; // nothing to delete on s3

            // Determine key from URL or path
            var cfg = GetConfigFromEnv();
            var key = ExtractKeyFromUrl(pathOrUrl, cfg);
            if (key == null) return true;

            return await client.DeleteFileAsync(key);
        }

        private static CloudStorageConfig GetConfigFromEnv()
        {
            return new CloudStorageConfig
            {
                BucketName = Environment.GetEnvironmentVariable("S3_BUCKET") ?? string.Empty,
                AccessKey = Environment.GetEnvironmentVariable("S3_ACCESS_KEY") ?? string.Empty,
                SecretKey = Environment.GetEnvironmentVariable("S3_SECRET_KEY") ?? string.Empty,
                ServiceUrl = Environment.GetEnvironmentVariable("S3_SERVICE_URL"),
                PublicBaseUrl = Environment.GetEnvironmentVariable("S3_PUBLIC_BASE_URL"),
                ForcePathStyle = string.Equals(Environment.GetEnvironmentVariable("S3_FORCE_PATH_STYLE"), "true", StringComparison.OrdinalIgnoreCase)
            };
        }

        private static string? ExtractKeyFromUrl(string urlOrPath, CloudStorageConfig cfg)
        {
            if (string.IsNullOrWhiteSpace(urlOrPath)) return null;

            // If it's already a key-like path without scheme and not starting with '/', return it
            if (!urlOrPath.StartsWith("http", StringComparison.OrdinalIgnoreCase) && !urlOrPath.StartsWith("/"))
                return urlOrPath;

            // If PublicBaseUrl configured and url starts with it
            if (!string.IsNullOrWhiteSpace(cfg.PublicBaseUrl) && urlOrPath.StartsWith(cfg.PublicBaseUrl.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
            {
                var prefix = cfg.PublicBaseUrl.TrimEnd('/') + "/";
                return urlOrPath.Substring(prefix.Length);
            }

            // If ServiceUrl is set
            if (!string.IsNullOrWhiteSpace(cfg.ServiceUrl) && urlOrPath.StartsWith(cfg.ServiceUrl.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
            {
                var baseUrl = cfg.ServiceUrl.TrimEnd('/');
                var remainder = urlOrPath.Substring(baseUrl.Length).TrimStart('/');
                if (cfg.ForcePathStyle)
                {
                    // remainder may start with "{bucket}/{key}"
                    var bucketPrefix = cfg.BucketName + "/";
                    if (remainder.StartsWith(bucketPrefix))
                        return remainder.Substring(bucketPrefix.Length);
                    return remainder; // fallback
                }
                else
                {
                    // remainder is key
                    return remainder;
                }
            }

            // AWS default pattern: https://{bucket}.s3.{region}.amazonaws.com/{key}
            var awsPrefix = $"https://{cfg.BucketName}.s3.";
            if (urlOrPath.StartsWith(awsPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var idx = urlOrPath.IndexOf('/', awsPrefix.Length);
                if (idx >= 0) return urlOrPath.Substring(idx + 1);
            }

            // If it's a local path starting with '/', treat as key without bucket
            if (urlOrPath.StartsWith("/"))
                return urlOrPath.TrimStart('/');

            return null; // can't determine key
        }
    }
}
