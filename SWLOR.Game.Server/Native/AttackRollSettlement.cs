using NWN.Native.API;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Character;
using static SWLOR.NWN.API.NWScript.NWScript;
using SWLOR.Game.Server.Composition;
using static SWLOR.Game.Server.Composition.GameServices;
using static SWLOR.Game.Server.Native.ResolveAttackRoll;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// What the final result of a native weapon roll settles: Embattled, the engine's defensive effects, the Force
    /// conversion stance's upkeep, the opening attack modifiers, the queued weapon ability or the attempt notification,
    /// and the combat recording.
    /// </summary>
    public static class AttackRollSettlement
    {
        internal static void SettleAttackRoll(CNWSCreature attacker, CNWSCreature defender, CNWSObject targetObject,
            CNWSCombatAttackData pAttackData, CNWSCombatRound pCombatRound, CNWSItem weapon, SkillType weaponSkillType,
            SkillType abilitySkillType, AbilityDetail queuedAbility, RangedWeaponAttackContext.Roll shot, bool isHit,
            int hitRate, int autoAttackCycleCriticalRate)
        {
            // Embattled ramps from every attempted hostile attack (hit, miss, or deflect), not only
            // landed hits. Refresh no-ops unless the defender owns the trait.
            if (GetIsReactionTypeHostile(attacker.m_idSelf, defender.m_idSelf))
                EmbattledStatusEffect.Refresh(defender.m_idSelf, attacker.m_idSelf);

            Log.Write(LogGroup.Attack, $"Resolving NWN defensive effects");
            // Resolve any defensive effects (like concealment).  Do this after all the above so that the attack data is
            // accurate.
            var wasSuccessfulBeforeDefensiveEffects = IsSuccessfulAttackResult(pAttackData.m_nAttackResult);
            attacker.ResolveDefensiveEffects(defender, isHit ? 1 : 0);
            if (wasSuccessfulBeforeDefensiveEffects &&
                !IsSuccessfulAttackResult(pAttackData.m_nAttackResult))
            {
                GameServices.CombatDamageTaken.TrackAvoidedAttack(defender.m_idSelf, attacker.m_idSelf);
            }

            using (CombatRecorder.BeginStatTriggerAttribution(attacker.m_idSelf, StatType.StanceHostileAutoAttackFPCost))
                GetDamageRoll.SettleForceConversionStanceAttack(
                    attacker, targetObject, IsSuccessfulAttackResult(pAttackData.m_nAttackResult));

            if (!IsSuccessfulAttackResult(pAttackData.m_nAttackResult))
                GameServices.CombatAbilityBonus.ClearOpeningAutoAttackModifiers(attacker.m_idSelf);

            // A landed queued weapon ability is finalized through Ability.EndAbilityImpact,
            // which sends the single attempt notification for that originating swing. A miss
            // or deflection never starts the queued ability, so notify it here instead.
            var queuedWeaponAbilityWillResolve =
                IsSuccessfulAttackResult(pAttackData.m_nAttackResult) &&
                UsePerkFeat.HasQueuedWeaponAbility(attacker.m_idSelf, weaponSkillType, attacker.m_pcCombatRound.m_nCurrentAttack);
            if (!queuedWeaponAbilityWillResolve && !shot.Ordinary)
            {
                // An avoided roll did not consume the readied ability. It still
                // consumes an ordinary attempted-shot charge, including Twin Suns.
                shot.Ordinary = true;
                shot.Bonuses = PairedRangedTraits.RecordOrdinaryAttempt(attacker.m_idSelf, targetObject.m_idSelf, shot.Id);
            }
            if (queuedWeaponAbilityWillResolve)
            {
                UsePerkFeat.ReserveQueuedWeaponAbilityAttack(
                    attacker.m_idSelf, pCombatRound.m_nCurrentAttack, weapon?.m_idSelf ?? OBJECT_INVALID);
                GameServices.CombatAbilityBonus.StoreQueuedWeaponAbilityCriticalRateBonus(
                    attacker.m_idSelf,
                    abilitySkillType,
                    autoAttackCycleCriticalRate);
            }
            else
            {
                StatusEffect.NotifyAttackAttemptStatusEffects(
                    attacker.m_idSelf,
                    weaponSkillType);
            }

            if (CombatRecorder.IsRecording)
                CombatRecorder.RecordAttack(attacker.m_idSelf, defender.m_idSelf, pAttackData.m_nAttackResult, hitRate,
                    queuedAbility?.Name);
        }
    }
}
