using System;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.NWN.API;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiList<T> : GuiWidget<T, GuiList<T>>
        where T: IGuiViewModel
    {
        private GuiListTemplate<T> Template { get; set; }

        private int RowCount { get; set; }
        private string RowCountBindName { get; set; }
        private bool IsRowCountBound => !string.IsNullOrWhiteSpace(RowCountBindName);

        private float RowHeight { get; set; }
        private bool ShowBorder { get; set; }
        private NuiScrollbars Scrollbars { get; set; }

        /// <summary>
        /// Sets a static value for the row count.
        /// </summary>
        /// <param name="rowCount">The number of rows</param>
        public GuiList<T> SetRowCount(int rowCount)
        {
            RowCount = rowCount;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the row count.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiList<T> BindRowCount<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            foreach (var cell in Template.Elements)
            {
                foreach (var element in cell.Elements)
                {
                    element.VisibilityOverrideBindName = GuiHelper<T>.GetPropertyName(expression) + "_RowVisibility";
                }
            }

            RowCountBindName = GuiHelper<T>.GetPropertyName(expression) + "_RowCount";
            return this;
        }

        /// <summary>
        /// Sets a static value for the height of the rows within the list.
        /// </summary>
        /// <param name="rowHeight">The height of the rows</param>
        public GuiList<T> SetRowHeight(float rowHeight)
        {
            RowHeight = rowHeight;
            return this;
        }

        /// <summary>
        /// Sets a static value for whether to show the borders.
        /// </summary>
        /// <param name="showBorders">true to display the borders, false otherwise</param>
        public GuiList<T> SetShowBorders(bool showBorders)
        {
            ShowBorder = showBorders;
            return this;
        }

        /// <summary>
        /// Sets a static value for the scrollbars to display.
        /// </summary>
        /// <param name="scrollbars">The type of scrollbars to display, if any.</param>
        public GuiList<T> SetScrollbars(NuiScrollbars scrollbars)
        {
            Scrollbars = scrollbars;
            return this;
        }

        public GuiList(GuiListTemplate<T> template)
        {
            Template = template;
            RowHeight = NuiStyle.RowHeight;
            ShowBorder = true;
            Scrollbars = NuiScrollbars.Y;

            Elements.Add(Template);
        }

        /// <summary>
        /// Builds the GuiList element.
        /// </summary>
        /// <returns>Json representing the list element.</returns>
        public override Json BuildElement()
        {
            var template = Template.ToJson();
            var rowCount = IsRowCountBound ? Nui.Bind(RowCountBindName) : JsonInt(RowCount);

            var json = Nui.List(template, rowCount, RowHeight, ShowBorder, Scrollbars);
            return json;
        }
    }
}
