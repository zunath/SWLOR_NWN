using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature
{
    public static class EshanCivilianPatrol
    {
        private const string RouteVariable = "ESHAN_PATROL_ROUTE";
        private const string IndexVariable = "ESHAN_PATROL_INDEX";
        private const string MovingVariable = "ESHAN_PATROL_MOVING";
        private const string PauseVariable = "ESHAN_PATROL_PAUSE_SECONDS";
        private const float ArrivalDistance = 0.75f;

        /// <summary>
        /// Walks only opted-in civilians around consecutive authored waypoints. Route tags must
        /// be globally unique and all points must remain in the civilian's current area.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureHeartbeatAfter)]
        public static void Heartbeat()
        {
            var creature = OBJECT_SELF;
            var route = ParseRoute(GetLocalString(creature, RouteVariable));
            if (route.Length == 0)
            {
                if (GetLocalInt(creature, MovingVariable) == 1)
                    StopOwnMovement(creature, GetCurrentAction(creature));
                return;
            }

            if (!GetIsObjectValid(creature) || GetObjectType(creature) != ObjectType.Creature ||
                GetIsPC(creature) || GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var currentAction = GetCurrentAction(creature);
            var inCombat = GetIsInCombat(creature) ||
                           GetIsObjectValid(Enmity.GetHighestEnmityTarget(creature));
            var available = AI.IsCreatureAIEnabled(creature) && GetCommandable(creature) &&
                            !GetIsDead(creature) && GetCurrentHitPoints(creature) > 0;
            if (!available || inCombat)
            {
                // Combat and possession own their actions; never clear those queues.
                DeleteLocalInt(creature, MovingVariable);
                return;
            }

            var area = GetArea(creature);
            var waypoints = ResolveRoute(area, route);
            if (waypoints.Length == 0)
            {
                StopOwnMovement(creature, currentAction);
                return;
            }

            var inConversation = IsInConversation(creature) ||
                                 currentAction == ActionType.DialogObject ||
                                 HasTetheredInteraction(creature, area);
            var busy = Activity.IsBusy(creature);
            if (inConversation || busy)
            {
                StopOwnMovement(creature, currentAction);
                return;
            }

            if (!ShouldIssuePatrolAction(available, waypoints.Length > 0, inCombat,
                    inConversation, busy, currentAction))
                return;

            DeleteLocalInt(creature, MovingVariable);
            var index = NormalizeIndex(GetLocalInt(creature, IndexVariable), waypoints.Length);
            var waypoint = waypoints[index];
            if (GetDistanceBetween(creature, waypoint) <= ArrivalDistance)
            {
                SetLocalInt(creature, IndexVariable, NextIndex(index, waypoints.Length));
                var pause = PauseSeconds(GetLocalInt(creature, PauseVariable));
                AssignCommand(creature, () => ActionWait(pause));
                return;
            }

            SetLocalInt(creature, IndexVariable, index);
            SetLocalInt(creature, MovingVariable, 1);
            var destination = GetLocation(waypoint);
            AssignCommand(creature, () => ActionMoveToLocation(destination));
        }

        [NWNEventHandler(ScriptName.OnDialogStart)]
        public static void StopForDialogue()
        {
            var creature = OBJECT_SELF;
            if (GetObjectType(creature) == ObjectType.Creature &&
                ParseRoute(GetLocalString(creature, RouteVariable)).Length > 0 &&
                !GetIsDMPossessed(creature) && !GetIsInCombat(creature) &&
                !GetIsObjectValid(Enmity.GetHighestEnmityTarget(creature)))
                StopOwnMovement(creature, GetCurrentAction(creature));
        }

        private static uint[] ResolveRoute(uint area, IReadOnlyList<string> route)
        {
            var waypoints = new uint[route.Count];
            for (var index = 0; index < route.Count; index++)
            {
                var waypoint = GetObjectByTag(route[index]);
                if (!IsRoutePointValid(area, GetArea(waypoint), GetObjectType(waypoint),
                        GetIsObjectValid(waypoint), GetIsObjectValid(GetObjectByTag(route[index], 1))))
                    return Array.Empty<uint>();

                waypoints[index] = waypoint;
            }

            return waypoints;
        }

        private static bool HasTetheredInteraction(uint creature, uint area)
        {
            // Conversation menus and authored graphs share the NUI conversation window.
            // Other NPC interactions can open a different window tethered to the same owner.
            for (var observer = GetFirstObjectInArea(area, ObjectType.Creature);
                 GetIsObjectValid(observer);
                 observer = GetNextObjectInArea(area, ObjectType.Creature))
            {
                if (!GetIsPC(observer) && !GetIsDM(observer) && !GetIsDMPossessed(observer))
                    continue;

                if (Gui.HasOpenWindowTetheredTo(observer, creature))
                    return true;
            }

            return false;
        }

        private static void StopOwnMovement(uint creature, ActionType currentAction)
        {
            if (GetLocalInt(creature, MovingVariable) == 1 && currentAction == ActionType.MoveToPoint)
                ClearAllActions(false, creature);

            DeleteLocalInt(creature, MovingVariable);
        }

        private static string[] ParseRoute(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Array.Empty<string>();

            var tags = value.Split(',', StringSplitOptions.TrimEntries);
            if (tags.Length < 2 || tags.Length > 16 ||
                tags.Any(tag => string.IsNullOrWhiteSpace(tag) || tag.Length > 32) ||
                tags.Distinct(StringComparer.OrdinalIgnoreCase).Count() != tags.Length)
                return Array.Empty<string>();

            return tags;
        }

        private static bool IsRoutePointValid(uint area, uint waypointArea, ObjectType type,
            bool waypointValid, bool duplicateTag) =>
            area != OBJECT_INVALID && waypointValid && !duplicateTag &&
            type == ObjectType.Waypoint && waypointArea == area;

        private static bool ShouldIssuePatrolAction(bool available, bool routeAvailable, bool inCombat,
            bool inConversation, bool busy, ActionType currentAction) =>
            available && routeAvailable && !inCombat && !inConversation && !busy &&
            currentAction == ActionType.Invalid;

        private static int NormalizeIndex(int index, int count) => index >= 0 && index < count ? index : 0;

        private static int NextIndex(int index, int count) => count > 1 ? (NormalizeIndex(index, count) + 1) % count : 0;

        private static int PauseSeconds(int authoredSeconds) => Math.Clamp(authoredSeconds > 0 ? authoredSeconds : 6, 1, 30);
    }
}
