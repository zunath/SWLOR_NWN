using System.Collections.Generic;
using NWN.Native.API;
using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using SWLOR.Game.Server.Composition;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Native
{
    public static unsafe class OnAIActionAttackObject
    {
        internal const int ACTION_IN_PROGRESS = 1;
        internal const int ACTION_COMPLETE = 2;
        internal const int ACTION_FAILED = 3;

        internal const int SANCTUARY_SAVE_FAILED = 1;
        internal const float CNW_PATHFIND_TOLERANCE = 0.01f;

        internal const ushort AISTATE_CREATURE_ABLE_TO_GO_HOSTILE = 0x0080;
        internal const ushort AISTATE_CREATURE_USE_HANDS = 0x0004;

        internal const int NWANIMBASE_ANIM_PAUSE = 0;
        internal const int NWANIMBASE_ANIM_READY = 1;
        internal const int NWANIMBASE_ANIM_ATTACK = 9;
        internal const int FEEDBACK_ACTION_CANT_REACH_TARGET = 218;

        internal const int CSERVERAIMASTER_AIACTION_ATTACKOBJECT = 12;
        internal const int CSERVERAIMASTER_AIACTION_CHECKMOVETOOBJECTRADIUS = 17;
        internal const int CSERVERAIMASTER_AIACTION_CHANGEFACINGOBJECT = 19;
        internal const int CNWSOBJECTACTION_PARAMETER_INTEGER = 1;
        internal const int CNWSOBJECTACTION_PARAMETER_FLOAT = 2;
        internal const int CNWSOBJECTACTION_PARAMETER_OBJECT = 3;

        internal const float RANGED_LINE_REPOSITION_ARC_LENGTH = 2.0f;
        internal const float RANGED_LINE_REPOSITION_MAX_ANGLE = 0.7853982f;
        internal const float RANGED_LINE_REPOSITION_COMPLETION_RANGE = 0.25f;

        internal const int CNWSCOMBATROUND_TYPE_INVALID = 0;
        internal const int CNWSCOMBATROUND_TYPE_ATTACK = 1;
        internal const int CNWSCOMBATROUND_TYPE_REACTION = 3;
        internal const int CNWSCOMBATROUND_TYPE_COMSTEP = 4;
        internal const int CNWSCOMBATROUND_TYPE_COMSTEPFB = 5;
        internal const int CNWSCOMBATROUND_TYPE_EQUIP = 6;
        internal const int CNWSCOMBATROUND_TYPE_UNEQUIP = 7;
        internal const int CNWSCOMBATROUND_TYPE_PARRY = 8;

        internal const int WEAPON_ATTACK_TYPE_MAINHAND = 1;
        internal const int WEAPON_ATTACK_TYPE_OFFHAND = 2;

        internal static readonly Dictionary<uint, DateTime> _creatureAttackDelays = new();
        internal static readonly Dictionary<uint, float> _rangedRepositionDirections = new();

        [BootPhase(BootReload.NativeHook)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHook()
        {
            NativeHookAccess.Hooks.Enable(NativeHookId.OnAIActionAttackObject);
        }

        internal static BlockedLineMovementPlan CreateBlockedLineMovementPlan(
            float attackerX,
            float attackerY,
            float attackerZ,
            float targetX,
            float targetY,
            float targetZ,
            uint attackerId,
            float desiredAttackRange,
            float personalSpaceRange,
            bool isOutsideAttackRange,
            bool hasRangedWeapon)
        {
            if (isOutsideAttackRange)
            {
                return new BlockedLineMovementPlan(
                    targetX,
                    targetY,
                    targetZ,
                    desiredAttackRange,
                    desiredAttackRange,
                    true);
            }

            if (!hasRangedWeapon)
            {
                return new BlockedLineMovementPlan(
                    targetX,
                    targetY,
                    targetZ,
                    personalSpaceRange,
                    personalSpaceRange,
                    true);
            }

            var destination = GetRangedLineRepositionDestination(
                attackerX,
                attackerY,
                attackerZ,
                targetX,
                targetY,
                attackerId,
                desiredAttackRange);
            return new BlockedLineMovementPlan(
                destination.X,
                destination.Y,
                destination.Z,
                RANGED_LINE_REPOSITION_COMPLETION_RANGE,
                desiredAttackRange,
                false);
        }

        internal static RepositionDestination GetRangedLineRepositionDestination(
            float attackerX,
            float attackerY,
            float attackerZ,
            float targetX,
            float targetY,
            uint attackerId,
            float desiredAttackRange)
        {
            var x = attackerX - targetX;
            var y = attackerY - targetY;
            var radius = MathF.Sqrt(x * x + y * y);
            var direction = GetRangedRepositionDirection(attackerId);
            var maximumArcStep =
                2f * radius * MathF.Sin(RANGED_LINE_REPOSITION_MAX_ANGLE / 2f);

            if (radius <= CNW_PATHFIND_TOLERANCE ||
                maximumArcStep <= RANGED_LINE_REPOSITION_COMPLETION_RANGE)
            {
                return new RepositionDestination(
                    targetX,
                    targetY + direction * desiredAttackRange,
                    attackerZ);
            }

            var angle = MathF.Min(
                RANGED_LINE_REPOSITION_MAX_ANGLE,
                RANGED_LINE_REPOSITION_ARC_LENGTH / radius);
            var cosine = MathF.Cos(angle);
            var sine = MathF.Sin(angle) * direction;

            return new RepositionDestination(
                targetX + x * cosine - y * sine,
                targetY + x * sine + y * cosine,
                attackerZ);
        }

        internal static float GetRangedRepositionDirection(uint attackerId)
        {
            return _rangedRepositionDirections.TryGetValue(attackerId, out var direction)
                ? direction
                : (attackerId & 1) == 0
                    ? 1f
                    : -1f;
        }

        internal static void AlternateRangedRepositionDirection(uint attackerId)
        {
            _rangedRepositionDirections[attackerId] = -GetRangedRepositionDirection(attackerId);
        }

        internal readonly record struct BlockedLineMovementPlan(
            float DestinationX,
            float DestinationY,
            float DestinationZ,
            float PathCompletionRange,
            float AttackCheckRange,
            bool TrackAttackTarget);

        internal readonly record struct RepositionDestination(float X, float Y, float Z);

        internal static float ResolveWeaponEngagementRange(
            float weaponEngagementRange,
            float maxAttackRange,
            bool usesRangedWeapon)
        {
            if (!usesRangedWeapon)
                return weaponEngagementRange;

            if (float.IsNaN(maxAttackRange) ||
                float.IsInfinity(maxAttackRange) ||
                maxAttackRange <= CNW_PATHFIND_TOLERANCE)
            {
                return weaponEngagementRange;
            }

            var maximumUsableRange = maxAttackRange - CNW_PATHFIND_TOLERANCE;
            return Math.Min(weaponEngagementRange, maximumUsableRange);
        }

        internal static float ResolveEngineDesiredAttackRange(
            float desiredAttackRange,
            float maxAttackRange,
            bool usesRangedWeapon)
        {
            if (!usesRangedWeapon ||
                !float.IsNaN(desiredAttackRange) &&
                !float.IsInfinity(desiredAttackRange) &&
                desiredAttackRange > CombatRules.MeleeWeaponEngagementRange)
            {
                return desiredAttackRange;
            }

            if (float.IsNaN(maxAttackRange) ||
                float.IsInfinity(maxAttackRange) ||
                maxAttackRange <= CombatRules.MeleeWeaponEngagementRange)
            {
                return CombatRules.RangedWeaponEngagementRange;
            }

            var maximumUsableRange = maxAttackRange - CNW_PATHFIND_TOLERANCE;
            return Math.Min(CombatRules.RangedWeaponEngagementRange, maximumUsableRange);
        }

        internal static float ResolveMaximumAttackRange(
            float desiredAttackRange,
            float maxAttackRange,
            bool usesRangedWeapon)
        {
            if (!usesRangedWeapon)
                return maxAttackRange;

            if (float.IsNaN(maxAttackRange) ||
                float.IsInfinity(maxAttackRange) ||
                maxAttackRange <= CNW_PATHFIND_TOLERANCE)
            {
                return desiredAttackRange;
            }

            return Math.Max(maxAttackRange, desiredAttackRange);
        }

        internal static bool TryCancelAttackForCombatLeash(CNWSCreature pCreature, CNWSObjectActionNode pNode, uint target)
        {
            if (!AI.TryStartCombatLeashEvade(pCreature.m_idSelf, target))
                return false;

            _creatureAttackDelays.Remove(pCreature.m_idSelf);
            _rangedRepositionDirections.Remove(pCreature.m_idSelf);
            GameServices.CombatAttackTiming.ClearAttackSwingDebt(pCreature.m_idSelf);
            pCreature.ChangeAttackTarget(pNode, OBJECT_INVALID);
            return true;
        }

        internal static float MagnitudeSquared(Vector v)
        {
            return v.x * v.x + v.y * v.y + v.z * v.z;
        }

        internal static float Sqr(float x)
        {
            return x * x;
        }

        internal static bool IsAIState(ushort nAIState, CNWSCreature pCreature)
        {
            return ((pCreature.m_nAIState & nAIState) == nAIState);
        }
    }
}
