using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;
using Random = SWLOR.Game.Server.Service.Random;
using Vector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Feature
{
    public static class DantooineRuinFarmWildlife
    {
        public static void AttachHeartbeat(uint creature)
        {
            if (GetIsObjectValid(creature) && !GetIsPC(creature) && !GetIsDM(creature) &&
                !GetIsDMPossessed(creature) && GetTag(GetArea(creature)) == "Dandestroy")
                SetEventScript(creature, EventScript.Creature_OnHeartbeat, ScriptName.OnRuinFarmWildlifeHeartbeat);
        }

        [NWNEventHandler(ScriptName.OnRuinFarmWildlifeHeartbeat)]
        public static void Heartbeat()
        {
            var creature = OBJECT_SELF;
            if (!GetIsObjectValid(creature) || GetIsPC(creature) || GetIsDM(creature) ||
                GetIsDMPossessed(creature) || GetIsDead(creature))
                return;

            AI.CreatureHeartbeat();
            if (!AI.IsCreatureAIEnabled(creature) || GetTag(GetArea(creature)) != "Dandestroy" ||
                GetIsInCombat(creature) || IsInConversation(creature) || AI.IsLeashEvading(creature) ||
                GetIsObjectValid(Enmity.GetHighestEnmityTarget(creature)))
                return;
            for (var effect = GetFirstEffect(creature); GetIsEffectValid(effect); effect = GetNextEffect(creature))
            {
                var type = GetEffectType(effect);
                if (type == EffectTypeScript.Dazed || type == EffectTypeScript.Petrify)
                    return;
            }

            var area = GetArea(creature);
            var home = GetLocalLocation(creature, "HOME_LOCATION");
            var homePosition = GetPositionFromLocation(home);
            if (GetAreaFromLocation(home) != area || !IsSafePosition(homePosition))
                return;

            var current = GetPosition(creature);
            if (!IsSafePosition(current))
            {
                AssignCommand(creature, () =>
                {
                    ClearAllActions();
                    ActionForceMoveToLocation(home);
                });
                return;
            }

            if (GetCurrentAction(creature) == ActionType.MoveToPoint ||
                GetCurrentAction(creature) == ActionType.RandomWalk ||
                Vector3.DistanceSquared(current, homePosition) > 225f || Random.D100(1) > 40)
                return;

            var requested = new Vector3(
                Math.Clamp(current.X + Random.NextFloat(-2f, 2f), 0f, 124f),
                Math.Clamp(current.Y + Random.NextFloat(-2f, 2f), 0f, 150f), current.Z);
            // One direct-walk check per decision. A blocked choice waits for the next heartbeat.
            var nativeCreature = NWNXLib.g_pAppManager.m_pServerExoApp.GetCreatureByGameObjectID(creature);
            using var point = new Vector(requested.X, requested.Y, requested.Z);
            using var result = new Vector();
            if (nativeCreature.GetArea().ComputeSafeLocation(point, 0.5f, nativeCreature.m_pcPathfindInformation, 1, result) == 0)
                return;

            var destination = new Vector3(result.x, result.y, result.z);
            if (!IsSafePosition(destination) || Vector3.DistanceSquared(current, destination) < 0.25f ||
                Vector3.DistanceSquared(current, destination) > 9f ||
                Vector3.DistanceSquared(homePosition, destination) > 225f)
                return;
            AssignCommand(creature, () => ActionMoveToLocation(Location(area, destination, 0f)));
        }

        public static bool IsSafePosition(Vector3 position) =>
            position.X >= 0f && position.X <= 124f && position.Y >= 0f && position.Y <= 150f &&
            float.IsFinite(position.Z);
    }
}
