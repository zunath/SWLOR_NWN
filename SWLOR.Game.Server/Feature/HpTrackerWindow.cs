using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature
{
    /// <summary>
    /// Coordinates the HP Tracker NUI window: refreshes open windows when a tracker changes and
    /// periodically (so creatures moving in/out of range are added/removed), and cleans up trackers when
    /// a creature is destroyed or a tracked player logs out. Also owns the shared permission rules.
    ///
    /// Windows are refreshed directly via <see cref="Gui.GetPlayerWindow"/> (not
    /// <see cref="Gui.PublishRefreshEvent{T}"/>) so DM clients are included — the publish path early-returns
    /// for non-PCs, and DMs are the primary users of /hptracker.
    /// </summary>
    public static class HpTrackerWindow
    {
        /// <summary>
        /// Rebuilds every currently-open HP Tracker window (for players and DMs). Cheap to call after any
        /// tracker change; no-ops for players whose window is closed.
        /// </summary>
        public static void RefreshOpenWindows()
        {
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (!Gui.IsWindowOpen(player, GuiWindowType.HpTracker))
                    continue;

                var viewModel = Gui.GetPlayerWindow(player, GuiWindowType.HpTracker).ViewModel;
                (viewModel as IGuiRefreshable<HpTrackerRefreshEvent>)?.Refresh(new HpTrackerRefreshEvent());
            }
        }

        [NWNEventHandler(ScriptName.OnSwlorHeartbeat)]
        public static void RefreshOnHeartbeat()
        {
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

        // ---- Shared permission rules (used by both the chat commands and the window view model) ----

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
