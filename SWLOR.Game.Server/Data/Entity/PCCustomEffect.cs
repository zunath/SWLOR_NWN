
using System;

using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Data.Entity
{
    public class PCCustomEffect: IEntity
    {
        public PCCustomEffect()
        {
            ID = Guid.NewGuid();
            Data = "";
            CasterNWNObjectID = "";
        }

        [Key]
        public Guid ID { get; set; }
        public Guid PlayerID { get; set; }
        public CustomEffectType CustomEffectID { get; set; }
        public int Ticks { get; set; }
        public int EffectiveLevel { get; set; }
        public string Data { get; set; }
        public string CasterNWNObjectID { get; set; }
        public int? StancePerkID { get; set; }
    }
}
