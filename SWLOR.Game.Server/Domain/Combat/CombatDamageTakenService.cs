using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using static SWLOR.Game.Server.Domain.Combat.CombatRules;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The damage taken part of <c>Combat</c>: the modifiers of an incoming hit (immunity, PvP, the Leadership and generic
    /// reductions, the redirect and share to a status source, fatal damage prevention), the Guard damage reduction read and
    /// the physical-to-Force defense conversion. The recent guarded hits and deflections are remembered by the defense memory
    /// part; the after-damage effects, the reflections, the guarded hit and its effects and the guard retaliation are parts
    /// of their own.
    /// </summary>
    public class CombatDamageTakenService : ICombatDamageTakenService
    {
        public const int BaseGuardDamageReductionPercent = 20;
        public const int MaximumGuardDamageReductionPercent = 55;

        private readonly ICombatQueriesService _queries;
        private readonly ICombatResourcesService _resources;
        private readonly IStatService _stat;
        private readonly IStatusEffectService _statusEffect;
        private readonly ICombatRecorderService _combatRecorder;
        private readonly ICombatRecordDamageService _combatRecordDamage;
        private readonly IPvpCombatService _pvpCombat;
        private readonly IEnmityService _enmity;

        public CombatDamageTakenService(
            ICombatQueriesService queries,
            ICombatResourcesService resources,
            IStatService stat,
            IStatusEffectService statusEffect,
            ICombatRecorderService combatRecorder,
            ICombatRecordDamageService combatRecordDamage,
            IPvpCombatService pvpCombat,
            IEnmityService enmity)
        {
            _queries = queries;
            _resources = resources;
            _stat = stat;
            _statusEffect = statusEffect;
            _combatRecorder = combatRecorder;
            _combatRecordDamage = combatRecordDamage;
            _pvpCombat = pvpCombat;
            _enmity = enmity;
        }

        public int ApplyDamageTakenModifiers(
            uint defender,
            int damage,
            uint attacker = OBJECT_INVALID,
            CombatDamageType damageType = CombatDamageType.Physical,
            CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct,
            int targetStatusDamagePercentAdjustment = 0,
            bool isLandedAttack = true,
            bool typedLeadershipReductionAlreadyApplied = false)
        {
            if (damage <= 0)
                return damage;

            if (_queries.HasDamageImmunity(defender, damageType))
                return 0;

            // Damage passed on to a protector was already raised for the player who was hit, but it
            // still keeps the protector in PvP combat.
            if (deliveryType != CombatDamageDeliveryType.Transferred)
                damage = _pvpCombat.ApplyDamageIncrease(defender, attacker, damage, isLandedAttack);
            else if (isLandedAttack)
                _pvpCombat.RecordHit(attacker, defender);

            var leadershipPhysicalAdjustment = !typedLeadershipReductionAlreadyApplied &&
                                               damageType.IsPhysicalDamageType()
                ? GetLeadershipPhysicalDamageTakenPercentAdjustment(defender)
                : 0;
            var leadershipForceAdjustment = !typedLeadershipReductionAlreadyApplied &&
                                            damageType == CombatDamageType.Force
                ? GetLeadershipForceDamageTakenPercentAdjustment(defender)
                : 0;
            var leadershipOtherAdjustment = !typedLeadershipReductionAlreadyApplied &&
                                            !damageType.IsPhysicalDamageType() &&
                                            damageType != CombatDamageType.Force
                ? _stat.GetStatAdjustment(defender, StatType.LeadershipOtherDamageTakenPercentAdjustment)
                : 0;
            damage = ApplyDamageTakenPercentageModifiers(
                damage,
                damageType,
                leadershipPhysicalAdjustment,
                leadershipForceAdjustment,
                leadershipOtherAdjustment,
                _stat.GetStatAdjustment(defender, StatType.DamageTakenPercentAdjustment),
                typedLeadershipReductionAlreadyApplied,
                targetStatusDamagePercentAdjustment);

            damage += _stat.GetStatAdjustment(defender, StatType.DamageTakenFlatAdjustment);

            damage = Math.Max(1, damage);

            // The math above is pure; everything below consumes one-shot effects or deals real
            // damage to third parties. A swing the engine later discards must not burn a redirect
            // or fatal-prevention charge, transfer damage to a protector, or grant temporary HP.
            if (!isLandedAttack)
                return damage;

            damage = ApplyDamageTakenRedirectToStatusSource(defender, attacker, damage, damageType);
            if (deliveryType != CombatDamageDeliveryType.Transferred)
                damage = ApplyDamageTakenShareToStatusSource(defender, attacker, damage, damageType);

            if (damage <= 0)
                return 0;

            if (TryPreventFatalDamageAndGrantTemporaryHP(defender, damage, restoreToOneHP: false))
                return 0;

            ApplyLowHPTemporaryHPBeforeFatalDamage(defender, damage);
            _resources.RefreshDamageDerivedHealingTargetHP(attacker, defender);
            if (_combatRecorder.IsRecording)
                _combatRecorder.ExpectDeferredDamage(attacker, defender, damage, deliveryType);
            return damage;
        }

        private int ApplyDamageTakenPercentageModifiers(
            int damage,
            CombatDamageType damageType,
            int leadershipPhysicalAdjustment,
            int leadershipForceAdjustment,
            int leadershipOtherAdjustment,
            int genericAdjustment,
            bool typedLeadershipReductionAlreadyApplied,
            int targetStatusDamagePercentAdjustment)
        {
            // Direct damage applies this channel explicitly after physical-to-Force conversion.
            // Damage that bypasses that pipeline must still use the same separate stage so its
            // Leadership and generic reductions remain multiplicative.
            if (!typedLeadershipReductionAlreadyApplied)
            {
                damage = ApplyTypedLeadershipDamageTakenPercentageModifier(
                    damage,
                    damageType,
                    leadershipPhysicalAdjustment,
                    leadershipForceAdjustment,
                    leadershipOtherAdjustment);
            }

            return ApplyCombinedDamageTakenAdjustment(damage, targetStatusDamagePercentAdjustment, genericAdjustment);
        }

        public int ApplyTypedLeadershipDamageTakenModifier(
            uint defender,
            int damage,
            CombatDamageType damageType)
        {
            var leadershipPhysicalAdjustment = damageType.IsPhysicalDamageType()
                ? GetLeadershipPhysicalDamageTakenPercentAdjustment(defender)
                : 0;
            var leadershipForceAdjustment = damageType == CombatDamageType.Force
                ? GetLeadershipForceDamageTakenPercentAdjustment(defender)
                : 0;
            var leadershipOtherAdjustment = !damageType.IsPhysicalDamageType() &&
                                            damageType != CombatDamageType.Force
                ? _stat.GetStatAdjustment(defender, StatType.LeadershipOtherDamageTakenPercentAdjustment)
                : 0;
            return ApplyTypedLeadershipDamageTakenPercentageModifier(
                damage,
                damageType,
                leadershipPhysicalAdjustment,
                leadershipForceAdjustment,
                leadershipOtherAdjustment);
        }

        private int GetLeadershipPhysicalDamageTakenPercentAdjustment(uint defender)
        {
            return _stat.GetStatAdjustment(defender, StatType.LeadershipPhysicalDamageTakenPercentAdjustment) +
                   _stat.GetStatAdjustment(defender, StatType.LeadershipRecoveryPhysicalDamageTakenPercentAdjustment);
        }

        private int GetLeadershipForceDamageTakenPercentAdjustment(uint defender)
        {
            return _stat.GetStatAdjustment(defender, StatType.LeadershipForceDamageTakenPercentAdjustment) +
                   _stat.GetStatAdjustment(defender, StatType.LeadershipRecoveryForceDamageTakenPercentAdjustment);
        }

        private int ApplyTypedLeadershipDamageTakenPercentageModifier(
            int damage,
            CombatDamageType damageType,
            int leadershipPhysicalAdjustment,
            int leadershipForceAdjustment,
            int leadershipOtherAdjustment)
        {
            var adjustment = damageType.IsPhysicalDamageType()
                ? leadershipPhysicalAdjustment
                : damageType == CombatDamageType.Force
                    ? leadershipForceAdjustment
                    : leadershipOtherAdjustment;
            return adjustment == 0
                ? damage
                : ApplyPercentDamageAdjustment(damage, adjustment);
        }

        private int ApplyDamageTakenRedirectToStatusSource(
            uint defender,
            uint attacker,
            int damage,
            CombatDamageType damageType)
        {
            if (damage <= 0)
                return damage;

            var redirectPercent = _stat.GetStatAdjustment(defender, StatType.DamageTakenRedirectToStatusSourcePercent);
            if (redirectPercent <= 0)
                return damage;

            var redirectTarget = _statusEffect.GetStatusEffectSourceWithStat(
                defender,
                StatType.DamageTakenRedirectToStatusSourcePercent);
            if (!GetIsObjectValid(redirectTarget) || GetIsDead(redirectTarget) || redirectTarget == defender)
                return damage;

            var redirectedDamage = Math.Min(
                damage,
                GameMath.PercentOf(damage, Math.Min(100, redirectPercent)));

            _statusEffect.RemoveStatusEffectsWithStat(defender, StatType.DamageTakenRedirectToStatusSourcePercent, false);

            // The redirect target takes a real hit from the attacker, so it gets the same
            // treatment as the Share sibling below: the target's own damage-taken mitigation
            // applies, and the damage dispatches under the attacker so the combat log does not
            // show the covered ally hurting their protector.
            var finalRedirectedDamage = ApplyDamageTakenModifiers(
                redirectTarget,
                redirectedDamage,
                attacker,
                damageType,
                CombatDamageDeliveryType.Transferred);

            if (finalRedirectedDamage > 0)
            {
                var damageSource = GetIsObjectValid(attacker) ? attacker : defender;
                AssignCommand(
                    damageSource,
                    () => ApplyEffectToObject(
                        DurationType.Instant,
                        EffectDamage(finalRedirectedDamage, damageType.GetNWScriptDamageType()),
                        redirectTarget));
            }
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Holy_Aid), redirectTarget);

            if (finalRedirectedDamage > 0 &&
                GetIsObjectValid(attacker) &&
                GetIsReactionTypeHostile(attacker, redirectTarget))
            {
                _enmity.ModifyEnmity(redirectTarget, attacker, finalRedirectedDamage);
            }

            return damage - redirectedDamage;
        }

        private int ApplyDamageTakenShareToStatusSource(
            uint defender,
            uint attacker,
            int damage,
            CombatDamageType damageType)
        {
            if (damage <= 0)
                return damage;

            var sharePercent = _stat.GetStatAdjustment(defender, StatType.DamageTakenShareToStatusSourcePercent);
            if (sharePercent <= 0)
                return damage;

            var shareTarget = _statusEffect.GetStatusEffectSourceWithStat(
                defender,
                StatType.DamageTakenShareToStatusSourcePercent);
            if (!GetIsObjectValid(shareTarget) ||
                GetIsDead(shareTarget) ||
                shareTarget == defender ||
                GetArea(shareTarget) != GetArea(defender))
            {
                return damage;
            }

            var sharedDamage = Math.Min(
                damage,
                GameMath.PercentOf(damage, Math.Min(100, sharePercent)));

            // The bond itself carries the damage across, so the redirected portion always
            // arrives as Force damage regardless of what type the original hit dealt. The
            // share target mitigates it with Force defense, not the attacker's damage type.
            const CombatDamageType SharedDamageType = CombatDamageType.Force;
            var finalSharedDamage = ApplyDamageTakenModifiers(
                shareTarget,
                sharedDamage,
                attacker,
                SharedDamageType,
                CombatDamageDeliveryType.Transferred);

            if (finalSharedDamage > 0)
            {
                // The attacker dealt this damage, so it has to be dispatched under them for the
                // combat log to attribute it correctly - the mitigation above already treats them
                // as the source. Running it under the defender instead makes a warded ally look
                // like they damaged their own bond target. Fall back only if the attacker is gone.
                var damageSource = GetIsObjectValid(attacker) ? attacker : defender;
                AssignCommand(
                    damageSource,
                    () => ApplyEffectToObject(
                        DurationType.Instant,
                        EffectDamage(finalSharedDamage, SharedDamageType.GetNWScriptDamageType()),
                        shareTarget));
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Holy_Aid), shareTarget);
            }

            if (finalSharedDamage > 0 &&
                GetIsObjectValid(attacker) &&
                GetIsReactionTypeHostile(attacker, shareTarget))
            {
                _enmity.ModifyEnmity(shareTarget, attacker, finalSharedDamage);
            }

            return damage - sharedDamage;
        }

        public int ApplyDamageOverTimeTakenModifiers(uint defender, int damage, CombatDamageType damageType) =>
            ApplyDamageOverTimeTakenModifiers(defender, damage, damageType, out _);

        public int ApplyDamageOverTimeTakenModifiers(
            uint defender,
            int damage,
            CombatDamageType damageType,
            out int targetStatusDamagePercentAdjustment)
        {
            targetStatusDamagePercentAdjustment = 0;
            if (damage <= 0)
                return damage;

            var adjustment = 0;
            if (damageType.IsPhysicalDamageType())
                adjustment += _stat.GetStatAdjustment(defender, StatType.PhysicalDamageOverTimeTakenPercentAdjustment);

            if (damageType == CombatDamageType.Force)
                adjustment += _stat.GetStatAdjustment(defender, StatType.ForceDamageTakenPercentAdjustment);

            targetStatusDamagePercentAdjustment = Math.Max(-MaximumCombinedDamageReductionPercent, adjustment);
            return ApplyPercentDamageAdjustment(damage, targetStatusDamagePercentAdjustment);
        }

        public bool TryPreventFatalDamageAndGrantTemporaryHP(
            uint defender,
            int damage,
            bool restoreToOneHP)
        {
            if (!GetIsObjectValid(defender) || (damage <= 0 && !restoreToOneHP))
                return false;

            var temporaryHPPercent = _stat.GetStatAdjustment(defender, StatType.FatalDamageTemporaryHPPercent);
            var duration = _stat.GetStatAdjustment(defender, StatType.FatalDamageTemporaryHPDurationSeconds);
            if (temporaryHPPercent <= 0 || duration <= 0)
                return false;

            var currentHP = GetCurrentHitPoints(defender);
            var isIncomingDamageFatal = damage > 0 && currentHP > 0 && damage >= currentHP;
            var isDyingFallback = restoreToOneHP && currentHP <= 0;
            if (!isIncomingDamageFatal && !isDyingFallback)
                return false;

            var cooldown = _stat.GetStatAdjustment(defender, StatType.FatalDamageTemporaryHPCooldownSeconds);
            if (!_resources.TryUseStatTrigger(defender, StatType.FatalDamageTemporaryHPPercent, cooldown))
                return false;

            var scalingAbilityScore = _stat.GetStatAdjustment(defender, StatType.FatalDamageTemporaryHPScalingAbilityScore);
            var temporaryHPSource = _statusEffect.GetStatusEffectSourceWithStat(
                defender,
                StatType.FatalDamageTemporaryHPPercent);
            var tempHP = HealAmount.FromRate(temporaryHPPercent).ForMaxHitPoints(GetMaxHitPoints(defender));
            if (scalingAbilityScore > 0)
                tempHP = AbilityEffectScaling.ScaleRestorativeEffect(tempHP, scalingAbilityScore);

            _statusEffect.RemoveStatusEffectsWithStat(defender, StatType.FatalDamageTemporaryHPPercent, false);

            if (restoreToOneHP && currentHP <= 0)
            {
                if (_combatRecorder.IsRecording)
                    _combatRecordDamage.KeepLethalHitPoints(defender);
                SetCurrentHitPoints(defender, 1);
            }

            if (GetIsObjectValid(temporaryHPSource))
            {
                using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(temporaryHPSource, StatType.FatalDamageTemporaryHPPercent);
                TemporaryHitPointEffects.ApplyFlatFromSource(
                    temporaryHPSource,
                    defender,
                    TemporaryHitPointEffectKey.FatalDamageSave,
                    tempHP,
                    duration);
            }
            else
            {
                using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(defender, StatType.FatalDamageTemporaryHPPercent);
                TemporaryHitPointEffects.ApplyFlat(
                    defender,
                    TemporaryHitPointEffectKey.FatalDamageSave,
                    tempHP,
                    duration);
            }
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), defender);

            return true;
        }

        private void ApplyLowHPTemporaryHPBeforeFatalDamage(uint defender, int damage)
        {
            if (!GetIsObjectValid(defender) || GetIsDead(defender) || damage <= 0)
                return;

            var threshold = _stat.GetStatAdjustment(defender, StatType.LowHPTemporaryHPThresholdPercent);
            var temporaryHPPercent = _stat.GetStatAdjustment(defender, StatType.LowHPTemporaryHPPercent);
            var duration = _stat.GetStatAdjustment(defender, StatType.LowHPTemporaryHPDurationSeconds);
            var cooldown = _stat.GetStatAdjustment(defender, StatType.LowHPTemporaryHPCooldownSeconds);
            if (threshold <= 0 || temporaryHPPercent <= 0 || duration <= 0)
                return;

            var maxHP = GetMaxHitPoints(defender);
            var currentHP = GetCurrentHitPoints(defender);
            if (maxHP <= 0 || currentHP <= 0)
                return;

            var thresholdHP = maxHP * (threshold / 100f);
            var projectedHP = currentHP - damage;
            if (currentHP < thresholdHP || projectedHP >= thresholdHP || projectedHP > 0)
                return;

            if (!_resources.TryUseStatTrigger(defender, StatType.LowHPTemporaryHPPercent, cooldown))
                return;

            var temporaryHP = GameMath.PercentOf(maxHP, temporaryHPPercent);
            using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(defender, StatType.LowHPTemporaryHPPercent);
            TemporaryHitPointEffects.ApplyFlat(defender, "LOW_HP_SHIELD", temporaryHP, duration);
        }

        public bool IsHostileAttackSource(uint defender, uint attacker)
        {
            // Preserve PvP and DM-driven testing while rejecting clearly non-hostile NPC swings.
            if (GetIsPC(attacker) || GetIsDM(attacker) || GetIsDMPossessed(attacker))
                return true;

            return GetIsReactionTypeHostile(attacker, defender) ||
                   GetIsReactionTypeHostile(defender, attacker) ||
                   GetIsEnemy(attacker, defender) ||
                   GetIsEnemy(defender, attacker);
        }

        /// <summary>
        /// Retrieves the percentage of damage a successful Guard removes from an incoming hit,
        /// including stat adjustments and the effective minimum and maximum bounds.
        /// </summary>
        /// <param name="defender">The creature to check.</param>
        /// <returns>The damage reduction percentage applied on a guarded hit.</returns>
        public int GetGuardDamageReductionPercent(uint defender)
        {
            var adjustment = _stat.GetStatAdjustment(defender, StatType.GuardDamageReductionPercentAdjustment);
            return Math.Clamp(
                BaseGuardDamageReductionPercent + adjustment,
                0,
                MaximumGuardDamageReductionPercent);
        }

        /// <summary>
        /// First half of Saber Ward / Aegis Eternal conversion: give the converted share its Force Defense
        /// mitigation. The physical hit's damage roll uses a defense value blended between the defender's
        /// Physical Defense and Force Defense by the conversion percent, so the converted share is mitigated
        /// as Force. The single damage roll is RNG-based and consumes stateful modifiers, so it cannot be run
        /// twice per hit; blending the defense is how the Force-Defense mitigation is applied without a second
        /// roll. The re-typing itself — removing the converted share from the physical hit and dealing it as a
        /// real Force instance (Force resistance, combat-log visibility) — is done afterward by
        /// <see cref="ICombatDamageReflectionService.ApplyIncomingPhysicalToForceConversion"/>. The percentage is read from
        /// <see cref="StatType.IncomingPhysicalToForceConversionPercent"/> on the defender, and only
        /// physical-category damage is affected. The Force Defense value is resolved lazily because the
        /// auto-attack and ability damage paths look it up through different (native vs managed) helpers.
        /// </summary>
        public int ApplyIncomingPhysicalToForceDefenseConversion(
            uint defender,
            CombatDamageType damageType,
            int physicalDefense,
            Func<int> forceDefenseProvider)
        {
            if (!damageType.IsPhysicalDamageType())
                return physicalDefense;

            var conversionPercent = _stat.GetStatAdjustment(defender, StatType.IncomingPhysicalToForceConversionPercent);
            if (conversionPercent <= 0)
                return physicalDefense;

            conversionPercent = Math.Clamp(conversionPercent, 0, 100);
            var forceDefense = forceDefenseProvider();
            return (physicalDefense * (100 - conversionPercent) + forceDefense * conversionPercent) / 100;
        }
    }
}
