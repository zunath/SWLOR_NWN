using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PCMarketListing]")]
    public class PCMarketListing: IEntity
    {
        public PCMarketListing()
        {
            ID = Guid.NewGuid();
        }

        [ExplicitKey]
        public Guid ID { get; set; }
        public Guid SellerPlayerID { get; set; }
        public string Note { get; set; }
        public int Price { get; set; }
        public int MarketRegionID { get; set; }
        public int MarketCategoryID { get; set; }
        public DateTime DatePosted { get; set; }
        public DateTime DateExpires { get; set; }
        public DateTime? DateSold { get; set; }
        public DateTime? DateRemoved { get; set; }
        public Guid? BuyerPlayerID { get; set; }
        public string ItemID { get; set; }
        public string ItemName { get; set; }
        public string ItemTag { get; set; }
        public string ItemResref { get; set; }
        public string ItemObject { get; set; }
        public int ItemRecommendedLevel { get; set; }
        public int ItemStackSize { get; set; }

        public IEntity Clone()
        {
            return new PCMarketListing
            {
                ID = ID,
                SellerPlayerID = SellerPlayerID,
                Note = Note,
                Price = Price,
                MarketRegionID = MarketRegionID,
                MarketCategoryID = MarketCategoryID,
                DatePosted = DatePosted,
                DateExpires = DateExpires,
                DateSold = DateSold,
                DateRemoved = DateRemoved,
                BuyerPlayerID = BuyerPlayerID,
                ItemID = ItemID,
                ItemName = ItemName,
                ItemTag = ItemTag,
                ItemResref = ItemResref,
                ItemObject = ItemObject,
                ItemRecommendedLevel = ItemRecommendedLevel,
                ItemStackSize = ItemStackSize
            };
        }
    }
}
