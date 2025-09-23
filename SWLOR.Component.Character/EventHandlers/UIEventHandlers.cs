using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Component.Character.UI.Payload;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Component.Character.EventHandlers
{
    internal class UIEventHandlers
    {
        private IGuiService _gui;
        public UIEventHandlers(IGuiService gui)
        {
            _gui = gui;
        }


        /// <summary>
        /// Skips the default NWN window open events and shows the SWLOR windows instead.
        /// Applies to the Journal and Character Sheet.
        /// </summary>
        [ScriptHandler<OnModuleGuiEvent>]
        public void ReplaceNWNGuis()
        {
            var player = GetLastGuiEventPlayer();
            var type = GetLastGuiEventType();
            if (type != GuiEventType.DisabledPanelAttemptOpen) return;
            var target = GetLastGuiEventObject();

            var panelType = (GuiPanel)GetLastGuiEventInteger();
            if (panelType == GuiPanel.CharacterSheet)
            {
                // Player character sheet
                if (target == player)
                {
                    var payload = new CharacterSheetPayload(player, true);
                    _gui.TogglePlayerWindow(player, GuiWindowType.CharacterSheet, payload);
                }
                // Associate character sheet (droid, pet, etc.)
                else if (GetMaster(target) == player)
                {
                    var payload = new CharacterSheetPayload(target, false);
                    _gui.TogglePlayerWindow(player, GuiWindowType.CharacterSheet, payload);
                }
            }
            else if (panelType == GuiPanel.Journal)
            {
                _gui.TogglePlayerWindow(player, GuiWindowType.Quests);
            }
        }
    }
}
