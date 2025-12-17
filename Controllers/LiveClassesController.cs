using Microsoft.AspNetCore.Mvc;

namespace CareerCracker.Controllers
{
    public class LiveClassesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
