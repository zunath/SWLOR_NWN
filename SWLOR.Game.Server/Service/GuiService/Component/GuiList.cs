using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiList: GuiWidget
    {
        private GuiListTemplate Template { get; set; }

        private int RowCount { get; set; }
        private string RowCountBindName { get; set; }
        private bool IsRowCountBound => !string.IsNullOrWhiteSpace(RowCountBindName);

        private float RowHeight { get; set; }
        
        public GuiList SetRowCount(int rowCount)
        {
            RowCount = rowCount;
            return this;
        }

        public GuiList BindRowCount(string bindName)
        {
            RowCountBindName = bindName;
            return this;
        }

        public GuiList SetRowHeight(float rowHeight)
        {
            RowHeight = rowHeight;
            return this;
        }

        public GuiList(GuiListTemplate template)
        {
            Template = template;
            RowHeight = NuiStyle.RowHeight;
        }

        public override Json BuildElement()
        {
            var template = Template.ToJson();
            var rowCount = IsRowCountBound ? Nui.Bind(RowCountBindName) : JsonInt(RowCount);

            return Nui.List(template, rowCount, RowHeight);
        }
    }
}
