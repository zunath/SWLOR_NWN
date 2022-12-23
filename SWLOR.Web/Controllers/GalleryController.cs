using Microsoft.AspNetCore.Mvc;

namespace SWLOR.Web.Controllers
{
    public class GalleryController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
