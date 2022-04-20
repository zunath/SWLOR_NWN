using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SWLOR.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Error()
        {
            ViewData["RequestId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            return View();
        }
    }
}
