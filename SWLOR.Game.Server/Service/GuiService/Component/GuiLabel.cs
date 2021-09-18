using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiLabel: GuiWidget
    {
        private string Text { get; set; }
        private string TextBindName { get; set; }
        private bool IsTextBound => !string.IsNullOrWhiteSpace(TextBindName);
        
        private NuiHorizontalAlign HorizontalAlign { get; set; }
        private string HorizontalAlignBindName { get; set; }
        private bool IsHorizontalAlignBound => !string.IsNullOrWhiteSpace(HorizontalAlignBindName);
        
        private NuiVerticalAlign VerticalAlign { get; set; }
        private string VerticalAlignBindName { get; set; }
        private bool IsVerticalAlignBound => !string.IsNullOrWhiteSpace(VerticalAlignBindName);

        public GuiLabel SetText(string text)
        {
            Text = text;
            return this;
        }

        public GuiLabel BindText(string bindName)
        {
            TextBindName = bindName;
            return this;
        }

        public GuiLabel SetHorizontalAlign(NuiHorizontalAlign hAlign)
        {
            HorizontalAlign = hAlign;
            return this;
        }

        public GuiLabel BindHorizontalAlign(string bindName)
        {
            HorizontalAlignBindName = bindName;
            return this;
        }
        public GuiLabel SetVerticalAlign(NuiVerticalAlign vAlign)
        {
            VerticalAlign = vAlign;
            return this;
        }

        public GuiLabel BindVerticalAlign(string bindName)
        {
            VerticalAlignBindName = bindName;
            return this;
        }
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
