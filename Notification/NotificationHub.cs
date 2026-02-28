using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace CareerCracker.Notification
{
    public class NotificationHub : Hub
    {
        public async Task SendToAll(string message)
        {
            await Clients.All.SendAsync("ReceiveNotification", message);
        }
    }
}
