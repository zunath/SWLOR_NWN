
using System.Collections.Generic;

using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[SkillCategories]")]
    public class SkillCategory: IEntity
    {
        public SkillCategory()
        {
            Name = "";
        }

        [ExplicitKey]
        public int SkillCategoryID { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public int Sequence { get; set; }
    }
}
