using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiToggleButton: GuiWidget
    {
        public string Text { get; set; }
        public string TextBindName { get; set; }
        public bool IsTextBound => !string.IsNullOrWhiteSpace(TextBindName);

        public bool IsToggled { get; set; }
        public string IsToggledBindName { get; set; }
        public bool IsToggledBound => !string.IsNullOrWhiteSpace(IsToggledBindName);

        public override Json BuildElement()
        {
            var text = IsTextBound ? Nui.Bind(TextBindName) : JsonString(Text);
            var isToggled = IsToggledBound ? Nui.Bind(IsToggledBindName) : JsonBool(IsToggled);

            return Nui.ButtonSelect(text, isToggled);
        }
    }
}
