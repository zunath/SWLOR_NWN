using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Domain.World.Events;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.World.Feature
{
    public class HoloNetTerminal
    {
        private readonly IGuiService _guiService;

        public HoloNetTerminal(
            IGuiService guiService,
            IEventAggregator eventAggregator)
        {
            _guiService = guiService;

            // Subscribe to events
            eventAggregator.Subscribe<OnOpenHoloNet>(e => OpenHolonetUI());
        }

        /// <summary>
        /// When a user click the holonet terminal, a UI will open which lets them enter their message
        /// and pay a fee to send a message over the in-game holonet channel and discord.
        /// </summary>
        public void OpenHolonetUI()
        {
            var player = GetLastUsedBy();
            var terminal = OBJECT_SELF;
            _guiService.TogglePlayerWindow(player, GuiWindowType.HoloNet, null, terminal);
        }
    }
}
