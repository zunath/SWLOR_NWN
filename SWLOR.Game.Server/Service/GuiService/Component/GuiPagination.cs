using System.Linq.Expressions;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiPagination<T> : GuiWidget<T, GuiPagination<T>>
        where T : IGuiViewModel
    {
        private readonly GuiButton<T> _previousButton;
        private readonly GuiButton<T> _nextButton;
        private readonly GuiComboBox<T> _pageSelector;

        public GuiPagination(
            Expression<Func<T, GuiBindingList<GuiComboEntry>>> pageNumbers,
            Expression<Func<T, int>> selectedPageIndex,
            Expression<Func<T, Action>> previousPageAction,
            Expression<Func<T, Action>> nextPageAction)
        {
            Elements.Add(new GuiSpacer<T>());

            _previousButton = new GuiButton<T>();
            _previousButton
                .SetText("<")
                .SetWidth(32f)
                .SetHeight(35f)
                .BindOnClicked(previousPageAction);
            Elements.Add(_previousButton);

            _pageSelector = new GuiComboBox<T>();
            _pageSelector
                .BindOptions(pageNumbers)
                .BindSelectedIndex(selectedPageIndex);
            Elements.Add(_pageSelector);

            _nextButton = new GuiButton<T>();
            _nextButton
                .SetText(">")
                .SetWidth(32f)
                .SetHeight(35f)
                .BindOnClicked(nextPageAction);
            Elements.Add(_nextButton);

            Elements.Add(new GuiSpacer<T>());
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
            SetHeight(height);
            return this;
        }

        public override Json BuildElement()
        {
            var row = JsonArray();

            foreach (var element in Elements)
            {
                row = JsonArrayInsert(row, element.ToJson());
            }

            return Nui.Row(row);
        }
    }
}
