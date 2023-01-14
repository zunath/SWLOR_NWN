using Microsoft.AspNetCore.Mvc;

namespace SWLOR.Web.Controllers
{
    public class DownloadsController : Controller
    {
        private class Download
        {
            public string LocalPath { get; set; }
            public string ContentType { get; set; }
            public string FileName { get; set; }
        }

        private static readonly Dictionary<int, Download> _downloads = new Dictionary<int, Download>
        {
            {1, new Download { LocalPath = "/var/www/swlor_public_files/SWLOR GUI.zip", ContentType = "application/octet-stream", FileName = "SWLORGUI.zip"}}
        };

        public IActionResult Get(int id)
        {
            if (!_downloads.ContainsKey(id))
            {
                return Content("Download not found for ID " + id);
            }

            var download = _downloads[id];

            if (!System.IO.File.Exists(download.LocalPath))
            {
                return Content("File not found on server for ID " + id);
            }

            return File(System.IO.File.OpenRead(download.LocalPath), download.ContentType, download.FileName);
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
