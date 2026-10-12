using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature
{
    /// <summary>
    /// Opens the NUI DM toolbar for DMs. The native DM taskbar is moved off screen by the
    /// pnl_dm_taskbar model override in the sw_ui hak.
    /// </summary>
    public static class DMToolbarWindow
    {
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void OpenToolbar()
        {
            var dm = GetEnteringObject();
            if (!GetIsDM(dm))
                return;

            if (!Gui.IsWindowOpen(dm, GuiWindowType.DMToolbar))
                Gui.TogglePlayerWindow(dm, GuiWindowType.DMToolbar);
        }

        /// <summary>
        /// DM windows stay open across area changes, so ones that list the current area's objects refresh here.
        /// A DM possessing a creature changes areas through that creature.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void RefreshCurrentAreaWindows()
        {
            var dm = GetEnteringObject();
            if (GetIsDMPossessed(dm))
                dm = GetMaster(dm);

            if (GetIsDM(dm))
                Gui.PublishRefreshEvent(dm, new DMAreaChangedRefreshEvent());
        }

        /// <summary>
        /// Keeps the toolbar's Appear button in step when the DM uses the native radial menu instead.
        /// </summary>
        [NWNEventHandler(ScriptName.OnDMAppearAfter)]
        [NWNEventHandler(ScriptName.OnDMDisappearAfter)]
        public static void RefreshAppearButton()
        {
            var dm = OBJECT_SELF;
            if (GetIsDMPossessed(dm))
                dm = GetMaster(dm);

            if (GetIsDM(dm))
                Gui.PublishRefreshEvent(dm, new DMPresenceRefreshEvent());
        }
    }
}
