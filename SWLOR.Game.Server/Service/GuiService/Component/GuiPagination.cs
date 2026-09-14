using System.Linq.Expressions;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiPagination<T>
        where T : IGuiViewModel
    {
        private readonly GuiComboBox<T> _pageSelector;

        internal GuiPagination(
            GuiRow<T> row,
            Expression<Func<T, GuiBindingList<GuiComboEntry>>> pageNumbers,
            Expression<Func<T, int>> selectedPageIndex,
            Expression<Func<T, Action>> previousPageAction,
            Expression<Func<T, Action>> nextPageAction)
        {
            row.AddSpacer();

            row.AddButton()
                .SetText("<")
                .SetWidth(32f)
                .SetHeight(35f)
                .BindOnClicked(previousPageAction);

            _pageSelector = row.AddComboBox()
                .BindOptions(pageNumbers)
                .BindSelectedIndex(selectedPageIndex);

            row.AddButton()
                .SetText(">")
                .SetWidth(32f)
                .SetHeight(35f)
                .BindOnClicked(nextPageAction);

            row.AddSpacer();
        }

        public GuiPagination<T> SetPageSelectorWidth(float width)
        {
            _pageSelector.SetWidth(width);
            return this;
        }
    }
}
