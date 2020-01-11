
using System;
using Newtonsoft.Json;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Data.Entity
{
    [JsonObject(MemberSerialization.OptIn)]
    public class PCCustomEffect: IEntity
    {
        public PCCustomEffect()
        {
            ID = Guid.NewGuid();
            Data = "";
            CasterNWNObjectID = "";
        }

        [Key]
        [JsonProperty]
        public Guid ID { get; set; }
        [JsonProperty]
        public Guid PlayerID { get; set; }
        [JsonProperty]
        public CustomEffectType CustomEffectID { get; set; }
        [JsonProperty]
        public int Ticks { get; set; }
        [JsonProperty]
        public int EffectiveLevel { get; set; }
        [JsonProperty]
        public string Data { get; set; }
        [JsonProperty]
        public string CasterNWNObjectID { get; set; }
        [JsonProperty]
        public int? StancePerkID { get; set; }
    }
}
