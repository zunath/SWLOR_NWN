using System;
using Newtonsoft.Json;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Message: IEntity
    {
        public Message()
        {
            ID = Guid.NewGuid();
            DatePosted = DateTime.UtcNow;
        }

        [Key]
        [JsonProperty]
        public Guid ID { get; set; }
        [JsonProperty]
        public Guid BoardID { get; set; }
        [JsonProperty]
        public Guid PlayerID { get; set; }
        [JsonProperty]
        public string Title { get; set; }
        [JsonProperty]
        public string Text { get; set; }
        [JsonProperty]
        public DateTime DatePosted { get; set; }
        [JsonProperty]
        public DateTime DateExpires { get; set; }
        [JsonProperty]
        public DateTime? DateRemoved { get; set; }
    }
}
