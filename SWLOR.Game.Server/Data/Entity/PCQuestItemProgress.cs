

using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    public class PCQuestItemProgress: IEntity
    {
        public PCQuestItemProgress()
        {
            ID = Guid.NewGuid();
        }
        [Key]
        public Guid ID { get; set; }
        public Guid PlayerID { get; set; }
        public Guid PCQuestStatusID { get; set; }
        public string Resref { get; set; }
        public int Remaining { get; set; }
        public bool MustBeCraftedByPlayer { get; set; }
    }
}
