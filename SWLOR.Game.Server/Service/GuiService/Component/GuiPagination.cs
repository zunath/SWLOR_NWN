using System.Linq.Expressions;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiPagination<T>
        where T : IGuiViewModel
    {
        private readonly GuiRow<T> _row;
        private readonly GuiButton<T> _previousButton;
        private readonly GuiButton<T> _nextButton;
        private readonly GuiComboBox<T> _pageSelector;

        internal GuiPagination(
            GuiRow<T> row,
            Expression<Func<T, GuiBindingList<GuiComboEntry>>> pageNumbers,
            Expression<Func<T, int>> selectedPageIndex,
            Expression<Func<T, Action>> previousPageAction,
            Expression<Func<T, Action>> nextPageAction)
        {
            _row = row;
            _row.AddSpacer();

            _previousButton = _row.AddButton()
                .SetText("<")
                .SetWidth(32f)
                .SetHeight(35f)
                .BindOnClicked(previousPageAction);

            _pageSelector = _row.AddComboBox()
                .BindOptions(pageNumbers)
                .BindSelectedIndex(selectedPageIndex);

            _nextButton = _row.AddButton()
                .SetText(">")
                .SetWidth(32f)
                .SetHeight(35f)
                .BindOnClicked(nextPageAction);

            _row.AddSpacer();
        }

        public GuiPagination<T> SetPageSelectorWidth(float width)
        {
            _pageSelector.SetWidth(width);
            return this;
        }

        public GuiPagination<T> SetControlHeight(float height)
        {
            _previousButton.SetHeight(height);
            _pageSelector.SetHeight(height);
            _nextButton.SetHeight(height);
            _row.SetHeight(height);
            return this;
        }
    }
}
