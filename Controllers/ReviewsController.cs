using Microsoft.AspNetCore.Mvc;

namespace CareerCracker.Controllers
{
    public class ReviewsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
