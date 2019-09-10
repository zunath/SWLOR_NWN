

using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PCQuestKillTargetProgress]")]
    public class PCQuestKillTargetProgress: IEntity
    {
        public PCQuestKillTargetProgress()
        {
            ID = Guid.NewGuid();
        }
        [ExplicitKey]
        public Guid ID { get; set; }
        public Guid PlayerID { get; set; }
        public Guid PCQuestStatusID { get; set; }
        public int NPCGroupID { get; set; }
        public int RemainingToKill { get; set; }

        public IEntity Clone()
        {
            return new PCQuestKillTargetProgress
            {
                ID = ID,
                PlayerID = PlayerID,
                PCQuestStatusID = PCQuestStatusID,
                NPCGroupID = NPCGroupID,
                RemainingToKill = RemainingToKill
            };
        }
    }
}
