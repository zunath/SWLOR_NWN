using System;
using SWLOR.Game.Server.Service.PlayerMarketService;
using SWLOR.Shared.Abstractions;

namespace SWLOR.Game.Server.Entity
{
    public class MarketItem: EntityBase
    {
        [Indexed]
        public string MarketId { get; set; }
        public string MarketName { get; set; }
        [Indexed]
        public string PlayerId { get; set; }
        [Indexed]
        public string SellerName { get; set; }
        [Indexed]
        public int Price { get; set; }
        [Indexed]
        public bool IsListed { get; set; }
        [Indexed]
        public string Name { get; set; }
        public string Tag { get; set; }
        public string Resref { get; set; }
        public string Data { get; set; }
        public int Quantity { get; set; }
        public string IconResref { get; set; }
        [Indexed]
        public MarketCategoryType Category { get; set; }
        public DateTime? DateListed { get; set; }
    }
}
