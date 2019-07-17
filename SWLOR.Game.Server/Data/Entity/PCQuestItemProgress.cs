

using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PCQuestItemProgress]")]
    public class PCQuestItemProgress: IEntity
    {
        public PCQuestItemProgress()
        {
            ID = Guid.NewGuid();
        }
        [ExplicitKey]
        public Guid ID { get; set; }
        public Guid PlayerID { get; set; }
        public Guid PCQuestStatusID { get; set; }
        public string Resref { get; set; }
        public int Remaining { get; set; }
        public bool MustBeCraftedByPlayer { get; set; }

        public IEntity Clone()
        {
            return new PCQuestItemProgress
            {
                ID = ID,
                PlayerID = PlayerID,
                PCQuestStatusID = PCQuestStatusID,
                Resref = Resref,
                Remaining = Remaining,
                MustBeCraftedByPlayer = MustBeCraftedByPlayer
            };
        }
    }
}
