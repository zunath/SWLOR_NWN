using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiCheckBox: GuiWidget
    {
        public string Text { get; set; }
        public string TextBindName { get; set; }
        public bool IsTextBound => !string.IsNullOrWhiteSpace(TextBindName);

        public bool IsChecked { get; set; }
        public string IsCheckedBindName { get; set; }
        public bool IsCheckedBound => !string.IsNullOrWhiteSpace(IsCheckedBindName);

        public override Json BuildElement()
        {
            var text = IsTextBound ? Nui.Bind(TextBindName) : JsonString(Text);
            var isChecked = IsCheckedBound ? Nui.Bind(IsCheckedBindName) : JsonBool(IsChecked);

            return Nui.Check(text, isChecked);
        }
    }
}
