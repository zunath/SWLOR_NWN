

using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    public class PCSkill: IEntity
    {
        public PCSkill()
        {
            ID = Guid.NewGuid();
        }
        [Key]
        public Guid ID { get; set; }
        public Guid PlayerID { get; set; }
        public int SkillID { get; set; }
        public int XP { get; set; }
        public int Rank { get; set; }
        public bool IsLocked { get; set; }
    }
}
