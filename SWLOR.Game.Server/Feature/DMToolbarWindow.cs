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
        /// Moves the toolbar to whatever the DM controls next when they possess a creature or return to their avatar,
        /// so its buttons act for, and its state updates on, the creature the DM controls.
        /// </summary>
        [NWNEventHandler(ScriptName.OnDMPossessBefore)]
        [NWNEventHandler(ScriptName.OnDMPossessFullPowerBefore)]
        public static void MoveToolbarOnPossession()
        {
            // Possessing starts on the avatar; returning to it starts on the possessed creature. Afterwards the avatar
            // no longer reports as a DM, so the DM is identified here.
            var current = OBJECT_SELF;
            if (!GetIsDM(current) && !GetIsDMPossessed(current))
                return;

            var dm = DMPresence.GetDM(current);
            // Possession has not settled while the event runs, so follow it on the next tick. The client keeps the
            // window open across possession, so it is closed first to bind it to the creature the DM now controls.
            DelayCommand(0.1f, () =>
            {
                DMPresence.CloseWindow(dm, GuiWindowType.DMToolbar);
                DMPresence.ShowWindow(dm, GuiWindowType.DMToolbar);
            });
        }

        /// <summary>
        /// The Appear button cannot fade the avatar on the DM's screen while they possess a creature, so the fade is
        /// brought up to date when they return to the avatar.
        /// </summary>
        [NWNEventHandler(ScriptName.OnDMPossessBefore)]
        [NWNEventHandler(ScriptName.OnDMPossessFullPowerBefore)]
        public static void RefreshAvatarFadeOnRelease()
        {
            // Returning to the avatar starts on the possessed creature.
            var creature = OBJECT_SELF;
            if (!GetIsDMPossessed(creature))
                return;

            var dm = GetMaster(creature);
            // Possession has not ended while the event runs, so follow it on the next tick.
            DelayCommand(0.1f, () => DMPresence.RefreshAvatarFade(dm));
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
