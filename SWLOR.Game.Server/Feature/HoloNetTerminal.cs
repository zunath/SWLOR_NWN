using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature
{
    public class HolonetTerminal
    {
        /// <summary>
        /// When a user click the holonet terminal, a UI will open which lets them enter their message
        /// and pay a fee to send a message over the in-game holonet channel and discord.
        /// </summary>
        [NWNEventHandler(ScriptName.OnOpenHoloNet)]
        public static void OpenHolonetUI()
        {
            var player = GetLastUsedBy();
            var terminal = OBJECT_SELF;
            Gui.TogglePlayerWindow(player, GuiWindowType.HoloNet, null, terminal);
        }
    }
}