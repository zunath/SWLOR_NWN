

using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    public class PCMapProgression: IEntity
    {
        public PCMapProgression()
        {
            ID = Guid.NewGuid();
        }
        [Key]
        public Guid ID { get; set; }
        public Guid PlayerID { get; set; }
        public string AreaResref { get; set; }
        public string Progression { get; set; }
    }
}
