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
            var dm = DMPresence.GetDM(GetEnteringObject());
            if (!GetIsDM(dm))
                return;

            DMPresence.ShowWindow(dm, GuiWindowType.DMToolbar);
        }

        /// <summary>
        /// Moves the toolbar to whatever the DM controls when they possess a creature or return to their avatar.
        /// </summary>
        [NWNEventHandler(ScriptName.OnDMPossessAfter)]
        [NWNEventHandler(ScriptName.OnDMPossessFullPowerAfter)]
        public static void MoveToolbarOnPossession()
        {
            var self = OBJECT_SELF;
            var dm = GetIsDM(self) ? self : GetMaster(self);
            if (!GetIsDM(dm))
                return;

            // Possession has not settled while the event runs, so follow it on the next tick.
            DelayCommand(0.1f, () => DMPresence.ShowWindow(dm, GuiWindowType.DMToolbar));
        }

        /// <summary>
        /// DM windows stay open across area changes, so ones that list the current area's objects refresh here.
        /// A DM possessing a creature changes areas through that creature.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void RefreshCurrentAreaWindows()
        {
            var dm = DMPresence.GetDM(GetEnteringObject());
            if (!GetIsDM(dm))
                return;

            // The Chooser can be shown on the creature, on the avatar, or on a creature the DM possessed earlier.
            var controlled = DMPresence.GetControlledCreature(dm);
            foreach (var target in DMPresence.GetShownOn(controlled, GuiWindowType.DMChooser))
                Gui.PublishRefreshEvent(dm, new DMAreaChangedRefreshEvent(), target);
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

            if (!GetIsDM(dm))
                return;

            // The toolbar is shown on whatever the DM controls, which can be a possessed creature.
            foreach (var target in DMPresence.GetShownOn(DMPresence.GetControlledCreature(dm), GuiWindowType.DMToolbar))
                Gui.PublishRefreshEvent(dm, new DMPresenceRefreshEvent(), target);
        }
    }
}
