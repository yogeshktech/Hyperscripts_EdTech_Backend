using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CareerCracker.Services
{
    public class FirebaseService
    {
        public FirebaseService(IConfiguration configuration)
        {
            var keyPath = configuration["Firebase:ServiceAccountKeyPath"];

            if (string.IsNullOrEmpty(keyPath))
                throw new Exception("Firebase key path missing in appsettings.json");

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), keyPath);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Firebase key file not found at: {fullPath}");

            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.FromFile(fullPath)
                });
            }
        }

        public async Task<string> SendNotificationAsync(string deviceToken, string title, string body)
        {
            var message = new Message()
            {
                Token = deviceToken,
                Notification = new FirebaseAdmin.Messaging.Notification
                {
                    Title = title,
                    Body = body
                }
            };


            return await FirebaseMessaging.DefaultInstance.SendAsync(message);
        }
    }
}
