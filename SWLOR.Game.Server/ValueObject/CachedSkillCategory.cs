using SWLOR.Game.Server.Data;

namespace SWLOR.Game.Server.ValueObject
{
    public class CachedSkillCategory
    {
        public int SkillCategoryID { get; set; }
        public string Name { get; set; }

        public CachedSkillCategory(SkillCategory category)
        {
            SkillCategoryID = category.SkillCategoryID;
            Name = category.Name;
        }
    }
}
