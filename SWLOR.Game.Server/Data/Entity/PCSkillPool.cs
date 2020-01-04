using System;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Data.Entity
{
    public class PCSkillPool: IEntity
    {
        public PCSkillPool()
        {
            ID = Guid.NewGuid();
        }

        [Key]
        public Guid ID { get; set; }
        public Guid PlayerID { get; set; }
        public SkillCategory SkillCategoryID { get; set; }
        public int Levels { get; set; }
    }
}
