using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiSpacer: GuiWidget
    {
        public override Json BuildElement()
        {
            return Nui.Spacer();
        }
    }
}
