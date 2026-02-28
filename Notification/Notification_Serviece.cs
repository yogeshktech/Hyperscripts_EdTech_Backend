using FirebaseAdmin.Messaging;
using System.Threading.Tasks;

namespace CareerCracker.Notification
{
    public class NotificationService : INotificationService
    {
        public async Task<string> SendNotificationAsync(
            string deviceToken,
            string title,
            string body)
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

            return await FirebaseMessaging
                .DefaultInstance
                .SendAsync(message);
        }
    }
}
