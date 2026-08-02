using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature
{
    /// <summary>
    /// Coordinates the HP Tracker NUI window: refreshes open windows when a tracker changes and when a
    /// tracked creature or a viewer moves between areas, and cleans up trackers when a creature dies or
    /// is destroyed or a tracked player logs out. Also owns the shared permission rules.
    ///
    /// Windows are refreshed directly via <see cref="Gui.GetPlayerWindow"/> (not
    /// <see cref="Gui.PublishRefreshEvent{T}"/>) so DM clients are included — the publish path early-returns
    /// for non-PCs, and DMs are the primary users of /hptracker.
    /// </summary>
    public static class HPTrackerWindow
    {
        /// <summary>
        /// Rebuilds every currently-open HP Tracker window (for players and DMs). Cheap to call after any
        /// tracker change; no-ops for players whose window is closed.
        /// </summary>
        public static void RefreshOpenWindows()
        {
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (!Gui.IsWindowOpen(player, GuiWindowType.HPTracker))
                    continue;

                var viewModel = Gui.GetPlayerWindow(player, GuiWindowType.HPTracker).ViewModel;
                (viewModel as IGuiRefreshable<HPTrackerRefreshEvent>)?.Refresh(new HPTrackerRefreshEvent());
            }
        }

        /// <summary>
        /// The window lists tracked creatures in the viewer's area, so an area transition changes what open
        /// windows should show in exactly two cases: a tracked creature moved in or out, or a viewer with an
        /// open window changed areas. Every other refresh trigger is an explicit tracker change.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void RefreshOnAreaEnter()
        {
            RefreshIfListChanged(GetEnteringObject());
        }

        [NWNEventHandler(ScriptName.OnAreaExit)]
        public static void RefreshOnAreaExit()
        {
            RefreshIfListChanged(GetExitingObject());
        }

        private static void RefreshIfListChanged(uint mover)
        {
            // A DM possessing a creature transitions as that creature, but the window lives under the master.
            var windowHost = GetIsDMPossessed(mover) ? GetMaster(mover) : mover;

            if (HPTracker.Has(mover) || Gui.IsWindowOpen(windowHost, GuiWindowType.HPTracker))
                RefreshOpenWindows();
        }

        /// <summary>
        /// A dead creature drops its tracker at the moment of death. A corpse can linger valid-but-dead for
        /// a long time before OnObjectDestroyed fires, so death is the right trigger to stop tracking it.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureDeathAfter)]
        public static void RemoveTrackerOnDeath()
        {
            if (HPTracker.Remove(OBJECT_SELF))
                RefreshOpenWindows();
        }

        /// <summary>A destroyed creature drops its tracker (its object id can be reused).</summary>
        [NWNEventHandler(ScriptName.OnObjectDestroyed)]
        public static void RemoveTrackerOnDestroyed()
        {
            if (HPTracker.Remove(OBJECT_SELF))
                RefreshOpenWindows();
        }

        /// <summary>A logging-out player drops their tracker.</summary>
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void RemoveTrackerOnExit()
        {
            if (HPTracker.Remove(GetExitingObject()))
                RefreshOpenWindows();
        }

        // ---- Shared permission rules (used by both the chat command and the window view model) ----

        public static bool IsStaff(uint user)
        {
            if (GetIsDM(user) || GetIsDMPossessed(user))
                return true;

            var level = Authorization.GetAuthorizationLevel(user);
            return level == AuthorizationLevel.DM || level == AuthorizationLevel.Admin;
        }

        /// <summary>A trackable target is a valid, non-DM creature.</summary>
        public static bool IsTrackableTarget(uint creature)
        {
            return GetIsObjectValid(creature)
                   && GetObjectType(creature) == ObjectType.Creature
                   && !GetIsDM(creature);
        }

        /// <summary>Players may manage only themselves; staff may manage any trackable creature.</summary>
        public static bool CanManage(uint user, uint creature)
        {
            return IsTrackableTarget(creature) && (IsStaff(user) || creature == user);
        }
    }
}
