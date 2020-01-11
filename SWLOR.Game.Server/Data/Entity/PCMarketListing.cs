using System;
using Newtonsoft.Json;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Data.Entity
{
    [JsonObject(MemberSerialization.OptIn)]
    public class PCMarketListing: IEntity
    {
        public PCMarketListing()
        {
            ID = Guid.NewGuid();
        }

        [Key]
        public Guid ID { get; set; }
        [JsonProperty]
        public Guid SellerPlayerID { get; set; }
        [JsonProperty]
        public string Note { get; set; }
        [JsonProperty]
        public int Price { get; set; }
        [JsonProperty]
        public MarketRegion MarketRegionID { get; set; }
        [JsonProperty]
        public MarketCategory MarketCategoryID { get; set; }
        [JsonProperty]
        public DateTime DatePosted { get; set; }
        [JsonProperty]
        public DateTime DateExpires { get; set; }
        [JsonProperty]
        public DateTime? DateSold { get; set; }
        [JsonProperty]
        public DateTime? DateRemoved { get; set; }
        [JsonProperty]
        public Guid? BuyerPlayerID { get; set; }
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
        public int ItemRecommendedLevel { get; set; }
        [JsonProperty]
        public int ItemStackSize { get; set; }
    }
}
