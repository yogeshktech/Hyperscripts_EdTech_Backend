using HyperDroid.CloudKit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using POPI_TRACKER_BACKEND.S3Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CareerCracker.S3Services
{
    public static class S3StorageHelper
    {
        private static IConfiguration? _config;

        // ✅ Initialize in Program.cs
        public static void Initialize(IConfiguration configuration)
        {
            _config = configuration;
        }

        // ✅ Create S3 / MinIO Client
        private static CloudStorage? CreateClient()
        {
            var s3 = _config?.GetSection("S3");
            if (s3 == null) return null;

            var cfg = new CloudStorageConfig
            {
                BucketName = s3["BucketName"],
                AccessKey = s3["AccessKey"],
                SecretKey = s3["SecretKey"],
                ServiceUrl = s3["ServiceUrl"],          // http://116.203.133.249:9000
                PublicBaseUrl = s3["PublicBaseUrl"],    // same as above
                Region = s3["Region"],                  // keep for AWS compatibility
                ForcePathStyle = s3.GetValue<bool>("ForcePathStyle")
            };

            return new CloudStorage(cfg);
        }

        // =====================================================
        // ✅ UPLOAD FILE
        // =====================================================
        public static async Task<string?> UploadFileAsync(IFormFile file, string folder = "uploads")
        {
            if (file == null || file.Length == 0)
                return null;

            using var client = CreateClient();
            if (client == null)
                throw new Exception("S3 client not initialized");

            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            var key = $"{folder}/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}{ext}";

            await using var stream = file.OpenReadStream();

            await client.UploadStreamAsync(
                stream,
                key,
                file.FileName,
                file.ContentType,
                file.Length
            );

            var baseUrl = _config?["S3:PublicBaseUrl"]?.TrimEnd('/');
            var bucket = _config?["S3:BucketName"];

            return $"{baseUrl}/{bucket}/{key}";
        }

        // =====================================================
        // ✅ DELETE FILE (FIXED)
        // =====================================================
        public static async Task DeleteFileAsync(string? pathOrUrl)
        {
            if (string.IsNullOrWhiteSpace(pathOrUrl))
                return;

            using var client = CreateClient();
            if (client == null)
                return;

            var key = ExtractKey(pathOrUrl);
            if (string.IsNullOrEmpty(key))
                return;

            await client.DeleteFileAsync(key);
        }

        // =====================================================
        // ✅ EXTRACT KEY FROM URL
        // =====================================================
        private static string? ExtractKey(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            var baseUrl = _config?["S3:PublicBaseUrl"]?.TrimEnd('/');
            var bucket = _config?["S3:BucketName"];

            // Example:
            // http://116.203.133.249:9000/hyperdroid-storage/folder/file.jpg

            if (!string.IsNullOrEmpty(baseUrl) && url.StartsWith(baseUrl))
            {
                var path = url.Replace(baseUrl + "/", "");

                if (!string.IsNullOrEmpty(bucket) && path.StartsWith(bucket + "/"))
                {
                    return path.Substring(bucket.Length + 1);
                }

                return path;
            }

            return null;
        }
    }
}