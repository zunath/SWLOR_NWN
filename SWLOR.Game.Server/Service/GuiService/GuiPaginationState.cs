using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Service.GuiService
{
    public sealed class GuiPaginationState
    {
        public GuiBindingList<GuiComboEntry> PageNumbers { get; }
        public int SelectedPageIndex { get; }

        private GuiPaginationState(
            GuiBindingList<GuiComboEntry> pageNumbers,
            int selectedPageIndex)
        {
            PageNumbers = pageNumbers;
            SelectedPageIndex = selectedPageIndex;
        }

        public static GuiPaginationState Create(
            long totalRecordCount,
            int recordsPerPage,
            int selectedPageIndex)
        {
            if (totalRecordCount < 0)
                throw new ArgumentOutOfRangeException(nameof(totalRecordCount));
            if (recordsPerPage <= 0)
                throw new ArgumentOutOfRangeException(nameof(recordsPerPage));

            var pageCount = Math.Max(
                1,
                (int)Math.Ceiling(totalRecordCount / (double)recordsPerPage));
            var pageNumbers = new GuiBindingList<GuiComboEntry>();

            for (var pageIndex = 0; pageIndex < pageCount; pageIndex++)
            {
                pageNumbers.Add(new GuiComboEntry($"Page {pageIndex + 1}", pageIndex));
            }

            return new GuiPaginationState(
                pageNumbers,
                Math.Clamp(selectedPageIndex, 0, pageCount - 1));
        }
    }
}
