using Microsoft.AspNetCore.Mvc;

namespace SWLOR.Web.Controllers
{
    public class IntroController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}