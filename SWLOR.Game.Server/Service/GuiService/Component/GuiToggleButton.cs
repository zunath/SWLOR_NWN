using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiToggleButton: GuiWidget
    {
        private string Text { get; set; }
        private string TextBindName { get; set; }
        private bool IsTextBound => !string.IsNullOrWhiteSpace(TextBindName);
        
        private bool IsToggled { get; set; }
        private string IsToggledBindName { get; set; }
        private bool IsToggledBound => !string.IsNullOrWhiteSpace(IsToggledBindName);

        public GuiToggleButton SetText(string text)
        {
            Text = text;
            return this;
        }

        public GuiToggleButton BindText(string bindName)
        {
            TextBindName = bindName;
            return this;
        }

        public GuiToggleButton SetIsToggled(bool isToggled)
        {
            IsToggled = isToggled;
            return this;
        }

        public GuiToggleButton BindIsToggled(string bindName)
        {
            IsToggledBindName = bindName;
            return this;
        }

        public override Json BuildElement()
        {
            var text = IsTextBound ? Nui.Bind(TextBindName) : JsonString(Text);
            var isToggled = IsToggledBound ? Nui.Bind(IsToggledBindName) : JsonBool(IsToggled);

            return Nui.ButtonSelect(text, isToggled);
        }
    }
}
