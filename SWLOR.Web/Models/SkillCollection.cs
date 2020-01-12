using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;

namespace SWLOR.Web.Models
{
    public class SkillCollection: Dictionary<SkillCategory, Dictionary<Skill, SkillAttribute>>
    {
        public SkillCollection()
        {
            foreach (var category in SkillService.GetActiveCategories())
            {
                if(!ContainsKey(category))
                    Add(category, new Dictionary<Skill, SkillAttribute>());

                foreach (var skill in SkillService.GetAllSkillsInCategory(category))
                {
                    var skillDetails = SkillService.GetSkill(skill);
                    this[category].Add(skill, skillDetails);
                }
            }
        }
    }
}
