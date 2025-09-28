using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.Events.World;
using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Component.World.Feature
{
    public class HoloNetTerminal
    {
        private readonly IGuiService _guiService;

        public HoloNetTerminal(IGuiService guiService)
        {
            _guiService = guiService;
        }

        /// <summary>
        /// When a user click the holonet terminal, a UI will open which lets them enter their message
        /// and pay a fee to send a message over the in-game holonet channel and discord.
        /// </summary>
        [ScriptHandler<OnOpenHoloNet>]
        public void OpenHolonetUI()
        {
            var player = GetLastUsedBy();
            var terminal = OBJECT_SELF;
            _guiService.TogglePlayerWindow(player, GuiWindowType.HoloNet, null, terminal);
        }
    }
}
