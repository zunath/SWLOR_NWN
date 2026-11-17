using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AppearanceEditing
{
    /// <summary>Closes the appearance editor and its part browser when a DM possesses or releases a creature.</summary>
    public class AppearanceEditorWindowHandlers
    {
        private IGuiService Gui { get; }

        public AppearanceEditorWindowHandlers(IGuiService gui)
        {
            Gui = gui;
        }

        [NWNEventHandler(ScriptName.OnDMPossessBefore)]
        [NWNEventHandler(ScriptName.OnDMPossessFullPowerBefore)]
        public void CloseAppearanceWindowOnPossessionBefore()
        {
            var dm = OBJECT_SELF;
            var isUnpossess = StringToObject(EventsPlugin.GetEventData("TARGET")) == OBJECT_INVALID;

            if (isUnpossess)
            {
                var uiTarget = GetMaster(dm);

                Gui.CloseWindow(dm, GuiWindowType.AppearancePartBrowser, uiTarget);
                Gui.CloseWindow(dm, GuiWindowType.AppearanceEditor, uiTarget);
            }
            else
            {
                if (Gui.IsWindowOpen(dm, GuiWindowType.AppearanceEditor))
                    Gui.TogglePlayerWindow(dm, GuiWindowType.AppearanceEditor);
                Gui.ClosePlayerWindow(dm, GuiWindowType.AppearancePartBrowser);
            }
        }
    }
}
