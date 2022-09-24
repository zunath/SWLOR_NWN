using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Service
{
    public class DroidAssembly
    {
        [NWNEventHandler("droid_ass_used")]
        public static void UseDroidAssemblyTerminal()
        {
            var player = GetLastUsedBy();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            Gui.TogglePlayerWindow(player, GuiWindowType.DroidAssembly, null, OBJECT_SELF);
        }
    }
}
