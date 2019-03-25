using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.ValueObject
{
    public class PCMarketData
    {
        // Buy / Browse Mode
        public MarketBrowseMode BrowseMode { get; set; }
        public int BrowseCategoryID { get; set; }
        public Guid BrowsePlayerID { get; set; }
        public Guid BrowseListingID { get; set; }
        public bool IsConfirming { get; set; }
        public bool IsAccessingInventory { get; set; }
        public bool IsReturningFromItemPreview { get; set; }
        public Stack<DialogNavigation> TemporaryDialogNavigationStack { get; set; }

        // Sell / Manage Listings Mode
        public Guid ItemID { get; set; }
        public string ItemName { get; set; }
        public string ItemTag { get; set; }
        public string ItemResref { get; set; }
        public string ItemObject { get; set; }
        public int ItemRecommendedLevel { get; set; }
        public int ItemStackSize { get; set; }
        public int ItemMarketCategoryID { get; set; }
        public bool IsSellingItem { get; set; }
        public int SellPrice { get; set; }
        public bool IsReturningFromItemPicking { get; set; }
        public bool IsSettingSellerNote { get; set; }
        public string SellerNote { get; set; }
        public int LengthDays { get; set; }

        // Manage Listings Mode
        public Guid ManageListingID { get; set; }
        public bool IsListingExpired => ListingExpirationDate < DateTime.UtcNow;
        public DateTime ListingExpirationDate { get; set; }
    }
}
