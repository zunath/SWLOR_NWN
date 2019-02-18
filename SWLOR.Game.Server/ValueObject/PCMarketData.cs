using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.ValueObject
{
    public class PCMarketData
    {
        public NWItem Item { get; set; }
        public MarketBrowseMode BrowseMode { get; set; }
        public int BrowseCategoryID { get; set; }
        public Guid BrowsePlayerID { get; set; }
        public Guid BrowseListingID { get; set; }
        public bool IsConfirming { get; set; }
        public bool IsAccessingInventory { get; set; }
        public bool ReturningFromItemPreview { get; set; }
        public Stack<DialogNavigation> TemporaryDialogNavigationStack { get; set; }

    }
}
