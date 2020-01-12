using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SWLOR.Web.Controllers
{
    public class DownloadController : Controller
    {
        public IActionResult Index(int id)
        {
            var localPath = GetLocalPath(id);
            if (localPath == null)
            {
                return Content("Download not found for ID " + id);
            }

            if (!System.IO.File.Exists(localPath))
            {
                return Content("File not found on server for ID " + id);
            }

            var fileInfo = new FileInfo(localPath);

            // Normally we'd call "return File(...);" but some files are really big,
            // so we don't want to buffer them into memory before sending to the client.
            Response.Clear();
            Response.ContentType = "application/octet-stream";
            Response.ContentLength = fileInfo.Length;
            Response.Headers.Add("Content-Disposition", "attachment; filename=" + GetFileName(id));

            using (var stream = System.IO.File.OpenRead(localPath))
            {
                stream.CopyTo(Response.Body);
            }

            return Ok(Response);
        }

        private static string GetLocalPath(int id)
        {
            switch (id)
            {
                case 1:
                    return "/var/www/swlor_public_files/SWLOR GUI.rar";
                case 2:
                    return "/var/www/swlor_public_files/SWLOR Haks.rar";
                default:
                    return null;
            }
        }

        private static string GetFileName(int id)
        {
            switch (id)
            {
                case 1:
                    return "SWLORGUI.rar";
                case 2:
                    return "SWLORDevelopmentHaks.rar";
                default:
                    return null;
            }
        }

    }
}