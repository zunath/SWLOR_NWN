using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.ValueObject
{
    public class PCMarketData
    {
        public MarketBrowseMode BrowseMode { get; set; }
        public int BrowseCategoryID { get; set; }
        public Guid BrowsePlayerID { get; set; }
        public Guid BrowseListingID { get; set; }
        public bool IsConfirming { get; set; }
        public bool IsAccessingInventory { get; set; }
        public bool IsReturningFromItemPreview { get; set; }
        public Stack<DialogNavigation> TemporaryDialogNavigationStack { get; set; }

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
    }
}
