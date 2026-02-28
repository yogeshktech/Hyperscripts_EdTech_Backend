using System.Threading.Tasks;

namespace CareerCracker.Notification
{
    public interface INotificationService
    {
        Task<string> SendNotificationAsync(
            string deviceToken,
            string title,
            string body);
    }
}
