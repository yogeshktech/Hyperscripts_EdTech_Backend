using CareerCracker.Notification;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CareerCracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(
            INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> Send(
            string token,
            string title,
            string body)
        {
            var result = await _notificationService
                .SendNotificationAsync(token, title, body);

            return Ok(result);
        }
    }
}
