using Microsoft.AspNetCore.Mvc;

namespace MockOpenIdProvider.Controllers
{
    public class UserinfoController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
