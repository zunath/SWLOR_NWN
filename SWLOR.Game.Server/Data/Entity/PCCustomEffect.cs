
using System;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PCCustomEffects]")]
    public class PCCustomEffect: IEntity
    {
        public PCCustomEffect()
        {
            Data = "";
            CasterNWNObjectID = "";
        }

        [Key]
        public long PCCustomEffectID { get; set; }
        public string PlayerID { get; set; }
        public long CustomEffectID { get; set; }
        public int Ticks { get; set; }
        public int EffectiveLevel { get; set; }
        public string Data { get; set; }
        public string CasterNWNObjectID { get; set; }
        public int? StancePerkID { get; set; }
    }
}
