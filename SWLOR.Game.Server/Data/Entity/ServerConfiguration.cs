using System;
using Newtonsoft.Json;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ServerConfiguration: IEntity
    {
        public ServerConfiguration()
        {
            ServerName = "";
            MessageOfTheDay = "";
        }

        [Key]
        [JsonProperty]
        public int ID { get; set; }
        [JsonProperty]
        public string ServerName { get; set; }
        [JsonProperty]
        public string MessageOfTheDay { get; set; }
        [JsonProperty]
        public int DataVersion { get; set; }
        [JsonProperty]
        public DateTime LastGuildTaskUpdate { get; set; }
    }
}
