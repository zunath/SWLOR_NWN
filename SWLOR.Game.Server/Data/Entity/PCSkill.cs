

using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PCSkill]")]
    public class PCSkill: IEntity
    {
        public PCSkill()
        {
            ID = Guid.NewGuid();
        }
        [ExplicitKey]
        public Guid ID { get; set; }
        public Guid PlayerID { get; set; }
        public int SkillID { get; set; }
        public int XP { get; set; }
        public int Rank { get; set; }
        public bool IsLocked { get; set; }

        public IEntity Clone()
        {
            return new PCSkill
            {
                ID = ID,
                PlayerID = PlayerID,
                SkillID = SkillID,
                XP = XP,
                Rank = Rank,
                IsLocked = IsLocked
            };
        }
    }
}
