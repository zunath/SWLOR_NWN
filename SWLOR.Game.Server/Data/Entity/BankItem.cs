
using System;
using Newtonsoft.Json;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Data.Entity
{
    [JsonObject(MemberSerialization.OptIn)]
    public class BankItem: IEntity
    {
        public BankItem()
        {
            ID = Guid.NewGuid();
        }

        [Key]
        [JsonProperty]
        public Guid ID { get; set; }
        [JsonProperty]
        public Bank BankID { get; set; }
        [JsonProperty]
        public Guid PlayerID { get; set; }
        [JsonProperty]
        public string ItemID { get; set; }
        [JsonProperty]
        public string ItemName { get; set; }
        [JsonProperty]
        public string ItemTag { get; set; }
        [JsonProperty]
        public string ItemResref { get; set; }
        [JsonProperty]
        public string ItemObject { get; set; }
        [JsonProperty]
        public DateTime DateStored { get; set; }
    }
}
