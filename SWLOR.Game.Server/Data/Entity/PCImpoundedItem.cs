
using System;
using Newtonsoft.Json;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [JsonObject(MemberSerialization.OptIn)]
    public class PCImpoundedItem: IEntity
    {
        public PCImpoundedItem()
        {
            ID = Guid.NewGuid();
        }
        [Key]
        [JsonProperty]
        public Guid ID { get; set; }
        [JsonProperty]
        public Guid PlayerID { get; set; }
        [JsonProperty]
        public string ItemName { get; set; }
        [JsonProperty]
        public string ItemTag { get; set; }
        [JsonProperty]
        public string ItemResref { get; set; }
        [JsonProperty]
        public string ItemObject { get; set; }
        [JsonProperty]
        public DateTime DateImpounded { get; set; }
        [JsonProperty]
        public DateTime? DateRetrieved { get; set; }
    }
}
