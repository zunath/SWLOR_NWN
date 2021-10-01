using System;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

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
        
        public GuiList<T> SetRowCount(int rowCount)
        {
            RowCount = rowCount;
            return this;
        }

        public GuiList<T> BindRowCount<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            RowCountBindName = GuiHelper<T>.GetPropertyName(expression) + "_RowCount";
            return this;
        }

        public GuiList<T> SetRowHeight(float rowHeight)
        {
            RowHeight = rowHeight;
            return this;
        }

        public GuiList(GuiListTemplate<T> template)
        {
            Template = template;
            RowHeight = NuiStyle.RowHeight;
        }

        public override Json BuildElement()
        {
            var template = Template.ToJson();
            var rowCount = IsRowCountBound ? Nui.Bind(RowCountBindName) : JsonInt(RowCount);

            var json = Nui.List(template, rowCount, RowHeight);
            return json;
        }
    }
}
