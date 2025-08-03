using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Feature
{
    public static class DisableDefaultGameWindows
    {
        /// <summary>
        /// When the player enters the server, disable default game windows.
        /// In most cases, these windows are replaced with custom versions.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void DisableWindows()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            // Spell Book - Completely unused
            SetGuiPanelDisabled(player, GuiPanel.SpellBook, true);

            // Character Sheet - A NUI replacement is used
            SetGuiPanelDisabled(player, GuiPanel.CharacterSheet, true);

            // Journal - A NUI replacement is used
            SetGuiPanelDisabled(player, GuiPanel.Journal, true);

            // Compass - Space is used by HP/FP/STM bars.
            SetGuiPanelDisabled(player, GuiPanel.Compass, true);
        }
    }
}
