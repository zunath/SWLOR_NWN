using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiLabel: GuiWidget
    {
        public string Text { get; set; }
        public string TextBindName { get; set; }
        public bool IsTextBound => !string.IsNullOrWhiteSpace(TextBindName);

        public NuiHorizontalAlign HorizontalAlign { get; set; }
        public string HorizontalAlignBindName { get; set; }
        public bool IsHorizontalAlignBound => !string.IsNullOrWhiteSpace(HorizontalAlignBindName);

        public NuiVerticalAlign VerticalAlign { get; set; }
        public string VerticalAlignBindName { get; set; }
        public bool IsVerticalAlignBound => !string.IsNullOrWhiteSpace(VerticalAlignBindName);

        public GuiLabel()
        {
            Text = string.Empty;
            HorizontalAlign = NuiHorizontalAlign.Center;
            VerticalAlign = NuiVerticalAlign.Middle;
        }

        public override Json BuildElement()
        {
            var text = IsTextBound ? Nui.Bind(TextBindName) : JsonString(Text);
            var hAlign = IsHorizontalAlignBound ? Nui.Bind(HorizontalAlignBindName) : JsonInt((int) HorizontalAlign);
            var vAlign = IsVerticalAlignBound ? Nui.Bind(VerticalAlignBindName) : JsonInt((int)VerticalAlign);

            return Nui.Label(text, hAlign, vAlign);
        }
    }
}
