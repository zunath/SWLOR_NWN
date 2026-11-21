using NWN.Native.API;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Character;
using AbilityType = SWLOR.NWN.API.NWScript.Enum.AbilityType;
using AttackType = SWLOR.Game.Server.Enumeration.AttackType;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;
using static SWLOR.NWN.API.NWScript.NWScript;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// The attacker's side of the native attack roll: the attacker's accuracy, the circumstance, range and combat mode
    /// modifiers, and the hit chance adjustments. <see cref="ResolveAttackRoll"/> rolls against them.
    /// </summary>
    public static class AttackRollAccuracy
    {
        // Combat mode constants
        private const int PowerAttackMode = 2;
        private const int ImprovedPowerAttackMode = 3;

        // Modifier constants
        private const int CircumstanceBonus = 5;
        private const int PowerAttackPenalty = -5;
        private const int ImprovedPowerAttackPenalty = -10;
        private const int CloseRangePenalty = -20;
        private const int LongRangePenalty = -20;
        private const int MediumRangePenalty = -10;
        private const int ShortRangePenalty = -5;

        // Range constants
        private const float CloseRange = 5.0f;
        private const float ShortRange = 20.0f;
        private const float MediumRange = 30.0f;
        private const float LongRange = 40.0f;

        // NPC object ID constant
        private const uint NpcActionTargetId = 2130706432;

        internal static int GetAttackerAccuracy(CNWSCreature attacker, CNWSCreature defender, CNWSObject targetObject,
            CNWSItem weapon, AbilityDetail queuedAbility, SkillType abilitySkillType, int techniqueAccuracy,
            RangedWeaponAttackContext.Roll shot)
        {
            var accuracyAbility = queuedAbility == null
                ? AbilityType.Invalid
                : GameServices.CombatQueries.GetQueuedAbilityAccuracyAbilityType(attacker.m_idSelf, abilitySkillType);
            var capturedOrdinaryAccuracy = queuedAbility == null
                ? RangedWeaponAttackContext.GetProfile(attacker.m_idSelf)?.NativeAccuracy : null;
            var attackerAccuracy = capturedOrdinaryAccuracy ??
                CombatRating.GetAccuracyNative(attacker, weapon, abilitySkillType, accuracyAbility);
            if (queuedAbility == null)
                attackerAccuracy += techniqueAccuracy - Stat.GetStatAdjustment(attacker.m_idSelf, StatType.OrdinaryWeaponAccuracyPenalty);
            else
                attackerAccuracy += MeleeTechnique.GetAbilityAccuracy(attacker.m_idSelf, targetObject.m_idSelf, queuedAbility);
            if (shot.Ordinary)
                attackerAccuracy += shot.Bonuses.AccuracyAdjustment;
            else
                attackerAccuracy += UsePerkFeat.GetQueuedWeaponTraitAccuracy(attacker.m_idSelf);
            attackerAccuracy = GameServices.CombatDamageModifiers.ApplyStatusSourceAccuracyModifiers(
                attacker.m_idSelf,
                defender.m_idSelf,
                attackerAccuracy);
            return attackerAccuracy;
        }

        internal static int GetAccuracyModifiers(uint attackType, CNWSCreature attacker, CNWSCreature defender,
            CNWSItem weapon)
        {
            // Modifiers - put in modifiers here based on the type of attack (and type of weapon etc.).
            var accuracyModifiers = 0;
            // Overwatch is a next ranged attack trigger. Resolve it in the native
            // auto-attack path as well as the casted-ability path, and only when the
            // selected defender is the suppressed target that armed the trigger.
            // Defender not targeting the attacker.
            // Dev note: the GetItem method always creates a new instance of CNWActionNode so there should be no NPEs.
            // Note: this always returns object invalid for NPCs (2130706432) as their actions aren't represented the same way.
            var oidTarget = defender.m_pActionQueue.GetItem(0).oidTarget;

            if (oidTarget == OBJECT_INVALID)
            {
                oidTarget = (uint)defender.m_ScriptVars.GetInt(new CExoString("I_LAST_ATTACKED"));
            }

            // If this is an NPC attacking, Store the attack on the NPC.
            if (attacker.m_pActionQueue.GetItem(0).oidTarget == NpcActionTargetId)
            {
                Log.Write(LogGroup.Attack, "NPC attacking - storing target " + defender.m_idSelf);
                attacker.m_ScriptVars.SetInt(new CExoString("I_LAST_ATTACKED"), (int)defender.m_idSelf);
            }

            // oidTarget will be 0 for a newly spawned NPC who hasn't been attacked yet.  Don't let them get taken by surprise in round 1.
            if (oidTarget != 0 && oidTarget != attacker.m_idSelf)
            {
                Log.Write(LogGroup.Attack, "Defender current target (" + oidTarget + ") is not attacker (" + attacker.m_idSelf + "). Assign circumstance bonus");
                accuracyModifiers += CircumstanceBonus;
            }

            // Range bonuses and penalties
            accuracyModifiers += CalculateRangeModifiers(attackType, attacker, defender, weapon);

            // Combat Mode - Power Attack (-5 ACC)
            if (attacker.m_nCombatMode == PowerAttackMode)
            {
                accuracyModifiers += PowerAttackPenalty;
                Log.Write(LogGroup.Attack, $"Applying Power Attack penalty: {PowerAttackPenalty}");
            }
            // Combat Mode - Improved Power Attack (-10 ACC)
            else if (attacker.m_nCombatMode == ImprovedPowerAttackMode)
            {
                accuracyModifiers += ImprovedPowerAttackPenalty;
                Log.Write(LogGroup.Attack, $"Applying Imp. Power Attack penalty: {ImprovedPowerAttackPenalty}");
            }

            return accuracyModifiers;
        }

        internal static int GetHitChanceModifier(CNWSCreature attacker, CNWSCreature defender,
            SkillType weaponSkillType, SkillType abilitySkillType, AbilityDetail queuedAbility)
        {
            var queuedWeaponAbilityLongRangeHitChanceAdjustment =
                UsePerkFeat.HasQueuedWeaponAbility(attacker.m_idSelf, weaponSkillType, attacker.m_pcCombatRound.m_nCurrentAttack)
                    ? GameServices.CombatSuppression.GetRangedAbilityLongRangeHitChanceAdjustment(
                        attacker.m_idSelf,
                        defender.m_idSelf,
                        abilitySkillType)
                    : 0;
            var hitChanceModifier =
                GameServices.CombatRateModifiers.GetSideAttackHitChanceAdjustment(attacker.m_idSelf, defender.m_idSelf, abilitySkillType) +
                queuedWeaponAbilityLongRangeHitChanceAdjustment +
                GameServices.CombatAbilityHit.GetHitChanceAgainstSunderedTargetAdjustment(attacker.m_idSelf, defender.m_idSelf) +
                (queuedAbility == null ? 0 : GameServices.CombatAbilityBonus.GetQueuedWeaponAbilityActivationHitChanceAdjustment(
                    attacker.m_idSelf,
                    abilitySkillType,
                    queuedAbility)) +
                GameServices.CombatSuppression.ConsumeSuppressionRangedAttackAccuracyAdjustment(
                    attacker.m_idSelf,
                    defender.m_idSelf,
                    weaponSkillType);
            return hitChanceModifier;
        }

        private static int CalculateRangeModifiers(uint attackType, CNWSCreature attacker, CNWSCreature defender, CNWSItem weapon)
        {
            if (attackType != (uint)AttackType.Ranged)
                return 0;

            var attackerPos = attacker.m_vPosition;
            var defenderPos = defender.m_vPosition;

            // Calculate distance using X/Y coordinates only
            var range = Math.Sqrt(Math.Pow(attackerPos.x - defenderPos.x, 2) + Math.Pow(attackerPos.y - defenderPos.y, 2));

            Log.Write(LogGroup.Attack, $"Ranged attack at range {range}");

            // Close range (under 5.0)
            if (range < CloseRange)
            {
                if (weapon != null)
                    return CloseRangePenalty;
            }
            // Long range (over 40.0)
            else if (range > LongRange)
            {
                if (weapon != null && !Item.RifleBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem))
                    return LongRangePenalty;
                else
                    return MediumRangePenalty;
            }
            // Medium range (30.0 - 40.0)
            else if (range > MediumRange)
            {
                if (weapon != null && !Item.RifleBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem))
                    return MediumRangePenalty;
                else
                    return ShortRangePenalty;
            }
            // Short range (20.0 - 30.0)
            else if (weapon != null && range > ShortRange && !Item.RifleBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem))
            {
                return ShortRangePenalty;
            }

            return 0;
        }
    }
}
