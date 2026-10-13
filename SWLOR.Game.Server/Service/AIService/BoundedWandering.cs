using SWLOR.Game.Server.Native;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using Vector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Service.AIService
{
    public static class BoundedWandering
    {
        private const string EnabledVariable = "AI_WANDER_BOUNDED";
        private const string MinXVariable = "AI_WANDER_MIN_X";
        private const string MinYVariable = "AI_WANDER_MIN_Y";
        private const string MaxXVariable = "AI_WANDER_MAX_X";
        private const string MaxYVariable = "AI_WANDER_MAX_Y";
        private const string MoveVariable = "AI_WANDER_MOVE";
        private const string AreaVariable = "AI_WANDER_AREA";

        public static void SetBounds(uint creature, WanderBounds bounds)
        {
            if (!GetIsObjectValid(creature) || GetObjectType(creature) != ObjectType.Creature ||
                GetIsPC(creature) || GetIsDM(creature))
                return;
            ArgumentNullException.ThrowIfNull(bounds);
            SetLocalFloat(creature, MinXVariable, bounds.MinX);
            SetLocalFloat(creature, MinYVariable, bounds.MinY);
            SetLocalFloat(creature, MaxXVariable, bounds.MaxX);
            SetLocalFloat(creature, MaxYVariable, bounds.MaxY);
            SetLocalObject(creature, AreaVariable, GetArea(creature));
            SetLocalInt(creature, EnabledVariable, 1);
        }

        private static bool IsConfigured(uint creature) =>
            GetIsObjectValid(creature) && GetLocalInt(creature, EnabledVariable) == 1 &&
            !GetIsPC(creature) && !GetIsDM(creature) &&
            AI.GetAIFlag(creature).HasFlag(AIFlag.RandomWalk) &&
            GetLocalObject(creature, AreaVariable) == GetArea(creature);

        private static WanderBounds ReadBounds(uint creature)
        {
            var minX = GetLocalFloat(creature, MinXVariable);
            var minY = GetLocalFloat(creature, MinYVariable);
            var maxX = GetLocalFloat(creature, MaxXVariable);
            var maxY = GetLocalFloat(creature, MaxYVariable);
            return WanderBounds.IsValid(minX, minY, maxX, maxY)
                ? new WanderBounds(minX, minY, maxX, maxY)
                : null;
        }

        public static bool ReturnIfOutside(uint creature, Location home)
        {
            if (!IsConfigured(creature))
                return false;
            if (GetIsInCombat(creature) || IsInConversation(creature) ||
                GetIsObjectValid(Enmity.GetHighestEnmityTarget(creature)))
                return false;
            var bounds = ReadBounds(creature);
            if (bounds == null)
                return true; // Invalid opt-in configuration must never fall back to unbounded walking.
            if (bounds.Contains(GetPosition(creature)))
                return false;
            ReturnHome(creature, bounds, home);
            return true;
        }

        /// <returns>False only when the creature has not opted into bounded wandering.</returns>
        public static bool TryWander(uint creature, Location home, float homeRadius)
        {
            if (!IsConfigured(creature))
                return false;
            var bounds = ReadBounds(creature);
            if (bounds == null || GetIsInCombat(creature) || IsInConversation(creature) ||
                GetIsObjectValid(Enmity.GetHighestEnmityTarget(creature)) ||
                GetAreaFromLocation(home) != GetArea(creature))
                return true;

            var current = GetPosition(creature);
            var homePosition = GetPositionFromLocation(home);
            for (var attempt = 0; attempt < 8; attempt++)
            {
                var requested = bounds.Constrain(current + new Vector3(
                    Random.NextFloat(-WanderBounds.StepDistance, WanderBounds.StepDistance),
                    Random.NextFloat(-WanderBounds.StepDistance, WanderBounds.StepDistance), 0f));
                if (!bounds.AllowsStep(current, homePosition, requested, homeRadius) ||
                    !CreatureNavigation.TryFindWalkableDestination(creature, requested, out var destination) ||
                    !bounds.AllowsStep(current, homePosition, destination, homeRadius))
                    continue;

                var area = GetArea(creature);
                var moveId = GetLocalInt(creature, MoveVariable) + 1;
                SetLocalInt(creature, MoveVariable, moveId);
                AssignCommand(creature, () => ActionMoveToLocation(Location(area, destination, 0f)));
                DelayCommand(0.25f, () => MonitorMove(creature, area, bounds, home, moveId));
                break;
            }
            return true;
        }

        private static void MonitorMove(uint creature, uint area, WanderBounds bounds, Location home, int moveId)
        {
            if (!GetIsObjectValid(creature) || GetIsDead(creature) || GetArea(creature) != area ||
                GetLocalInt(creature, MoveVariable) != moveId || !IsConfigured(creature))
                return;
            // Combat owns movement as soon as a target is acquired.
            if (GetIsInCombat(creature) || IsInConversation(creature) ||
                GetIsObjectValid(Enmity.GetHighestEnmityTarget(creature)))
                return;
            if (!bounds.Contains(GetPosition(creature)))
            {
                ReturnHome(creature, bounds, home);
                return;
            }
            if (GetCurrentAction(creature) == ActionType.MoveToPoint)
                DelayCommand(0.25f, () => MonitorMove(creature, area, bounds, home, moveId));
        }

        private static void ReturnHome(uint creature, WanderBounds bounds, Location home)
        {
            if (GetAreaFromLocation(home) != GetArea(creature) || !bounds.Contains(GetPositionFromLocation(home)))
                return;
            AssignCommand(creature, () =>
            {
                ClearAllActions();
                ActionForceMoveToLocation(home);
            });
        }
    }
}
