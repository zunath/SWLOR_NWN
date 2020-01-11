using System;
using Newtonsoft.Json;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Data.Entity
{
    [JsonObject(MemberSerialization.OptIn)]
    public class GuildTask: IEntity
    {
        [Key]
        [JsonProperty]
        public int ID { get; set; }
        [JsonProperty]
        public GuildType GuildID { get; set; }
        [JsonProperty]
        public int QuestID { get; set; }
        [JsonProperty]
        public int RequiredRank { get; set; }
        [JsonProperty]
        public bool IsCurrentlyOffered { get; set; }
    }
}
