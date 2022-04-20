using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SWLOR.Game.Server.Service;

namespace SWLOR.Web.Controllers
{
    public class DataExportController : Controller
    {
        public IActionResult Skills()
        {
            var skills = Skill.GetAllActiveSkills();
            var json = JsonConvert.SerializeObject(skills, Formatting.Indented);

            return File(Encoding.UTF8.GetBytes(json),
                "application/json", 
                "SkillsExport.json");
        }

        public IActionResult Perks()
        {
            var perks = Perk.GetAllActivePerks();

            var settings = new JsonSerializerSettings();
            settings.Formatting = Formatting.Indented;
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            var json = JsonConvert.SerializeObject(perks, Formatting.Indented);

            return File(Encoding.UTF8.GetBytes(json),
                "application/json",
                "PerksExport.json");
        }
    }
}