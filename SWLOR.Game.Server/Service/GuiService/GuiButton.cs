using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiButton: GuiWidget
    {
        public string Text { get; set; }
        public string TextBindName { get; set; }
        public bool IsTextBound => !string.IsNullOrWhiteSpace(TextBindName);

        public override Json BuildElement()
        {
            var text = IsTextBound ? Nui.Bind(TextBindName) : JsonString(Text);

            return Nui.Button(text);
        }
    }
}
