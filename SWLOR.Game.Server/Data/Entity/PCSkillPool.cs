using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PCSkillPool]")]
    public class PCSkillPool: IEntity
    {
        public PCSkillPool()
        {
            ID = Guid.NewGuid();
        }

        [ExplicitKey]
        public Guid ID { get; set; }
        public Guid PlayerID { get; set; }
        public int SkillCategoryID { get; set; }
        public int Levels { get; set; }

        public IEntity Clone()
        {
            return new PCSkillPool
            {
                ID = ID,
                PlayerID = PlayerID,
                SkillCategoryID = SkillCategoryID,
                Levels = Levels
            };
        }
    }
}
