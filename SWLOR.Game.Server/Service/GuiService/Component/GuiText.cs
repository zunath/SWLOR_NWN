using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiText: GuiWidget
    {
        private string Text { get; set; }
        private string TextBindName { get; set; }
        private bool IsTextBound => !string.IsNullOrWhiteSpace(TextBindName);

        public GuiText SetText(string text)
        {
            Text = text;
            return this;
        }

        public GuiText BindText(string bindName)
        {
            TextBindName = bindName;
            return this;
        }


        public override Json BuildElement()
        {
            var text = IsTextBound ? Nui.Bind(TextBindName) : JsonString(Text);

            return Nui.Text(text);
        }
    }
}
