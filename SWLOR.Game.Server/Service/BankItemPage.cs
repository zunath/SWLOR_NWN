using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// One page of a bank search: the items on the page, how many items match in total, and the page selector state.
    /// </summary>
    public sealed class BankItemPage
    {
        public List<InventoryItem> Items { get; }
        public long MatchCount { get; }
        public GuiBindingList<GuiComboEntry> PageNumbers { get; }
        public int PageIndex { get; }

        public BankItemPage(
            List<InventoryItem> items,
            long matchCount,
            GuiBindingList<GuiComboEntry> pageNumbers,
            int pageIndex)
        {
            Items = items;
            MatchCount = matchCount;
            PageNumbers = pageNumbers;
            PageIndex = pageIndex;
        }
    }
}
