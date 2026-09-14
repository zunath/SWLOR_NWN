using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SlicingService;

namespace SWLOR.Game.Server.Feature
{
    public static class SlicingTerminal
    {
        [NWNEventHandler(ScriptName.OnSlicingTerminal)]
        public static void UseTerminal()
        {
            var player = GetLastUsedBy();
            var terminal = OBJECT_SELF;
            var tier = GetLocalInt(terminal, SlicingSession.TierVariable);

            if (!SlicingSession.TryStart(player, terminal, SlicingSourceType.Terminal, tier, out var error))
            {
                SendMessageToPC(player, error);
                return;
            }

            var payload = new SlicingPayload(terminal, SlicingSourceType.Terminal, tier);
            Gui.TogglePlayerWindow(player, GuiWindowType.Slicing, payload, terminal);
        }
    }
}
