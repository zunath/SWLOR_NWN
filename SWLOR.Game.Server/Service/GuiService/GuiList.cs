using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiList: GuiWidget
    {
        public GuiListTemplate Template { get; set; }

        public int RowCount { get; set; }
        public string RowCountBindName { get; set; }
        public bool IsRowCountBound => !string.IsNullOrWhiteSpace(RowCountBindName);

        public float RowHeight { get; set; }

        public GuiList()
        {
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
