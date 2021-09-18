using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiButtonImage: GuiWidget
    {
        public string Resref { get; set; }
        public string ResrefBindName { get; set; }
        public bool IsResrefBound => !string.IsNullOrWhiteSpace(ResrefBindName);

        public override Json BuildElement()
        {
            var resref = IsResrefBound ? Nui.Bind(ResrefBindName) : JsonString(Resref);

            return Nui.ButtonImage(resref);
        }
    }
}
