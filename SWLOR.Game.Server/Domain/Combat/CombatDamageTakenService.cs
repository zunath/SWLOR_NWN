using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using Random = SWLOR.Game.Server.Service.Random;
using static SWLOR.Game.Server.Domain.Combat.CombatRules;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The damage taken part of <c>Combat</c>: the modifiers of an incoming hit (immunity, PvP, the Leadership and generic
    /// reductions, the redirect and share to a status source, fatal damage prevention), the effects of damage taken and of
    /// low HP, Guard and its riders, avoided attacks, deflection and Deflecting Return, and the incoming physical-to-Force
    /// conversion. The recent guarded hits and deflections are this instance's own tables.
    /// </summary>
    public class CombatDamageTakenService : ICombatDamageTakenService
    {
        public const int BaseGuardDamageReductionPercent = 20;
        public const int MaximumGuardDamageReductionPercent = 55;

        private readonly Dictionary<uint, DateTime> _recentGuardedHits = new();
        private readonly Dictionary<(uint Creature, DeflectionSource Source), DateTime> _recentDeflections = new();

        private readonly ICombatFeedbackService _feedback;
        private readonly ICombatQueriesService _queries;
        private readonly ICombatResourcesService _resources;
        private readonly ICombatAttackTimingService _attackTiming;
        private readonly ICombatActivityService _combatActivity;
        private readonly ICombatDamageModifiersService _damageModifiers;
        private readonly ICombatAbilityHitService _abilityHit;
        private readonly ICombatHitEffectsService _hitEffects;
        private readonly IStatService _stat;
        private readonly IStatusEffectService _statusEffect;
        private readonly ITemporaryStatModifierService _temporaryStatModifier;
        private readonly ICombatRecorderService _combatRecorder;
        private readonly IPvpCombatService _pvpCombat;
        private readonly IEnmityService _enmity;
        private readonly IMessagingService _messaging;
        private readonly IPlayerNameService _playerName;
        private readonly Lazy<ICombatService> _combat;

        public CombatDamageTakenService(
            ICombatFeedbackService feedback,
            ICombatQueriesService queries,
            ICombatResourcesService resources,
            ICombatAttackTimingService attackTiming,
            ICombatActivityService combatActivity,
            ICombatDamageModifiersService damageModifiers,
            ICombatAbilityHitService abilityHit,
            ICombatHitEffectsService hitEffects,
            IStatService stat,
            IStatusEffectService statusEffect,
            ITemporaryStatModifierService temporaryStatModifier,
            ICombatRecorderService combatRecorder,
            IPvpCombatService pvpCombat,
            IEnmityService enmity,
            IMessagingService messaging,
            IPlayerNameService playerName,
            Lazy<ICombatService> combat)
        {
            _feedback = feedback;
            _queries = queries;
            _resources = resources;
            _attackTiming = attackTiming;
            _combatActivity = combatActivity;
            _damageModifiers = damageModifiers;
            _abilityHit = abilityHit;
            _hitEffects = hitEffects;
            _stat = stat;
            _statusEffect = statusEffect;
            _temporaryStatModifier = temporaryStatModifier;
            _combatRecorder = combatRecorder;
            _pvpCombat = pvpCombat;
            _enmity = enmity;
            _messaging = messaging;
            _playerName = playerName;
            _combat = combat;
        }

        // CalculateDamage stays on Combat because it rolls the shared Random; guard retaliation damage reaches it through Combat.
        protected virtual int CalculateDamage(
            int attackerAttack,
            int attackerDMG,
            int attackerStat,
            int defenderDefense,
            int defenderStat,
            int critical,
            int deltaCap = 0) =>
            _combat.Value.CalculateDamage(attackerAttack, attackerDMG, attackerStat, defenderDefense, defenderStat, critical, deltaCap);

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

        public void ApplyDamageReflectionEffects(
            uint attacker,
            uint defender,
            int damage,
            CombatDamageType damageType)
        {
            if (damage <= 0 ||
                attacker == defender ||
                !GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                GetObjectType(attacker) != SWLOR.NWN.API.NWScript.Enum.ObjectType.Creature ||
                GetObjectType(defender) != SWLOR.NWN.API.NWScript.Enum.ObjectType.Creature)
            {
                return;
            }

            var adjustment = 0;
            if (damageType == CombatDamageType.Force)
            {
                ApplyForceDamageTakenEffects(defender);
                adjustment += _stat.GetStatAdjustment(defender, StatType.ForceDamageReflectionPercentAdjustment);
            }

            if (damageType == CombatDamageType.Physical)
                adjustment += _stat.GetStatAdjustment(defender, StatType.PhysicalDamageReflectionPercentAdjustment);

            if (damageType.IsElementalDamageType())
                adjustment += _stat.GetStatAdjustment(defender, StatType.ElementalDamageReflectionPercentAdjustment);

            if (adjustment <= 0)
                return;

            var reflectedDamage = GameMath.PercentOf(damage, adjustment);
            using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(defender, GetDamageReflectionStat(damageType));
            _hitEffects.ApplyTriggeredDamage(defender, attacker, reflectedDamage, damageType);
        }

        private void ApplyForceDamageTakenEffects(uint defender)
        {
            var forceDefense = _stat.GetStatAdjustment(defender, StatType.ForceDamageTakenForceDefense);
            var durationSeconds = _stat.GetStatAdjustment(defender, StatType.ForceDamageTakenForceDefenseDurationSeconds);
            if (forceDefense <= 0 || durationSeconds <= 0)
                return;

            _statusEffect.ApplyStatusEffect(
                defender,
                defender,
                new ForceWardingStatusEffect(forceDefense),
                durationSeconds,
                CombatDamageType.Force);
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
                    _combatRecorder.KeepLethalHitPoints(defender);
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

        [NWNEventHandler(ScriptName.OnCreatureDamagedAfter)]
        public void ApplyDamageTakenEffects()
        {
            var defender = OBJECT_SELF;
            var attacker = GetLastDamager(defender);
            var damage = GetTotalDamageDealt();

            ApplyDamageTakenEffects(defender, attacker, damage);
        }

        public void ApplyDamageTakenEffects(uint defender, uint attacker, int damage)
        {
            if (!GetIsObjectValid(defender) || damage <= 0)
                return;

            // A lethal hit interrupts too: subdual can stand the caster back up before the cast would finish.
            Feature.UsePerkFeat.InterruptActivationOnDamage(defender, attacker);
            if (GetIsDead(defender))
                return;

            var attackPercent = _stat.GetStatAdjustment(defender, StatType.DamageTakenAttackPercentAdjustment);
            var attackDuration = _stat.GetStatAdjustment(defender, StatType.DamageTakenAttackDurationSeconds);
            if (attackPercent != 0 && attackDuration > 0)
            {
                _temporaryStatModifier.Replace(
                    defender,
                    StatType.AttackPercentAdjustment,
                    attackPercent,
                    attackDuration,
                    StatType.DamageTakenAttackPercentAdjustment);
            }

            ApplyLowHPDamageTakenEffects(defender, damage);
            ApplyDamageTakenNextSkillAbilityDamage(defender);
            ApplyReversalCutReady(defender);
            _combatActivity.TrackRecentDamageTaken(defender);
            ApplyRecentDamageTargetHitEffects(defender, attacker);

            if (GetIsObjectValid(attacker) && GetIsReactionTypeHostile(attacker, defender))
            {
                _combatActivity.TrackHostileDefensiveCombatEntryActivity(defender, attacker);
                EmbattledStatusEffect.Refresh(defender, attacker);
            }
        }

        private void ApplyDamageTakenNextSkillAbilityDamage(uint defender)
        {
            var skillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                defender,
                StatType.DamageTakenNextSkillAbilitySkillType));
            var damageBonus = _stat.GetStatAdjustment(defender, StatType.DamageTakenNextSkillAbilityDamageBonus);
            var window = _stat.GetStatAdjustment(defender, StatType.DamageTakenNextSkillAbilityWindowSeconds);
            if (damageBonus <= 0 || window <= 0)
                return;

            var requiredCategory = GetStatusEffectCategoryFromStat(_stat.GetStatAdjustment(
                defender,
                StatType.DamageTakenNextSkillAbilityRequiredCategory));
            var wasArmed = _temporaryStatModifier.GetStatAdjustment(
                defender,
                StatType.NextSkillAbilityDamageBonus,
                StatType.NextSkillAbilitySkillType) == damageBonus;
            _attackTiming.GrantNextSkillAbilityBonuses(defender, skillType, damageBonus, 0, window, requiredCategory: requiredCategory);
            _attackTiming.ShowReadyCue(defender, wasArmed, damageBonus, ReadyCue.Damage("Breaker Reversal", damageBonus));
        }

        public void ApplyLowHPDamageTakenEffects(uint defender, int damage)
        {
            if (!GetIsObjectValid(defender) || GetIsDead(defender) || damage <= 0)
                return;

            ApplyLowHPPhysicalDefenseEffect(defender, damage);
            ApplyLowHPEvasionEffect(defender, damage);
            ApplyLowHPNextAbilityNoStaminaCostEffect(defender, damage);
            ApplyLowHPTemporaryHPEffect(defender, damage);
            ApplyLowHPNoSaveTemporaryHPEffect(defender, damage);
            ApplyLowHPGuardEffect(defender, damage);
        }

        private void ApplyReversalCutReady(uint defender)
        {
            if (_stat.GetStatAdjustment(defender, StatType.TwinBladeDuelistReversalCut) <= 0)
                return;

            var window = _stat.GetStatAdjustment(defender, StatType.TwinBladeDuelistReversalCutWindowSeconds);
            if (window <= 0)
                return;

            var damageBonus = _stat.GetStatAdjustmentExcludingTemporaryModifiers(
                defender,
                StatType.TwinBladeDuelistReversalCutDamageBonus);
            if (damageBonus > 0)
            {
                _temporaryStatModifier.Replace(
                    defender,
                    StatType.TwinBladeDuelistReversalCutDamageBonus,
                    damageBonus,
                    window,
                    StatType.TwinBladeDuelistReversalCut);
            }

            var dazedDuration = _stat.GetStatAdjustmentExcludingTemporaryModifiers(
                defender,
                StatType.TwinBladeDuelistReversalCutDazedDurationSeconds);
            if (dazedDuration > 0)
            {
                _temporaryStatModifier.Replace(
                    defender,
                    StatType.TwinBladeDuelistReversalCutDazedDurationSeconds,
                    dazedDuration,
                    window,
                    StatType.TwinBladeDuelistReversalCut);
            }
        }

        private void ApplyLowHPPhysicalDefenseEffect(uint defender, int damage)
        {
            var threshold = _stat.GetStatAdjustment(defender, StatType.LowHPPhysicalDefenseThresholdPercent);
            var defensePercent = _stat.GetStatAdjustment(defender, StatType.LowHPPhysicalDefensePercentAdjustment);
            var duration = _stat.GetStatAdjustment(defender, StatType.LowHPPhysicalDefenseDurationSeconds);
            var cooldown = _stat.GetStatAdjustment(defender, StatType.LowHPPhysicalDefenseCooldownSeconds);

            if (threshold <= 0 ||
                defensePercent == 0 ||
                duration <= 0 ||
                !_resources.DidCrossHPThreshold(defender, damage, threshold) ||
                !_resources.TryUseStatTrigger(defender, StatType.LowHPPhysicalDefensePercentAdjustment, cooldown))
                return;

            // A visible status effect carries the defense bonus so the player can track the trigger.
            _statusEffect.ApplyStatusEffect(
                defender,
                defender,
                new UnbreakableStatusEffect(defensePercent),
                duration);
        }

        private void ApplyLowHPEvasionEffect(uint defender, int damage)
        {
            var threshold = _stat.GetStatAdjustment(defender, StatType.LowHPEvasionThresholdPercent);
            var evasionPercent = _stat.GetStatAdjustment(defender, StatType.LowHPEvasionPercentAdjustment);
            var duration = _stat.GetStatAdjustment(defender, StatType.LowHPEvasionDurationSeconds);
            var cooldown = _stat.GetStatAdjustment(defender, StatType.LowHPEvasionCooldownSeconds);

            if (threshold <= 0 ||
                evasionPercent == 0 ||
                duration <= 0 ||
                !_resources.DidCrossHPThreshold(defender, damage, threshold) ||
                !_resources.TryUseStatTrigger(defender, StatType.LowHPEvasionPercentAdjustment, cooldown))
                return;

            _temporaryStatModifier.Replace(
                defender,
                StatType.EvasionPercentAdjustment,
                evasionPercent,
                duration,
                StatType.LowHPEvasionPercentAdjustment);
        }

        private void ApplyLowHPNextAbilityNoStaminaCostEffect(uint defender, int damage)
        {
            var threshold = _stat.GetStatAdjustment(defender, StatType.LowHPNextAbilityNoStaminaCostThresholdPercent);
            var skillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(defender, StatType.LowHPNextAbilityNoStaminaCostSkillType));
            var duration = _stat.GetStatAdjustment(defender, StatType.LowHPNextAbilityNoStaminaCostDurationSeconds);
            var cooldown = _stat.GetStatAdjustment(defender, StatType.LowHPNextAbilityNoStaminaCostCooldownSeconds);

            if (threshold <= 0 ||
                skillType == SkillType.Invalid ||
                duration <= 0 ||
                !_resources.DidCrossHPThreshold(defender, damage, threshold) ||
                !_resources.TryUseStatTrigger(defender, StatType.LowHPNextAbilityNoStaminaCostSkillType, cooldown))
                return;

            _temporaryStatModifier.Replace(
                defender,
                StatType.NextAbilityNoStaminaCostSkillType,
                (int)skillType,
                duration,
                StatType.NextAbilityNoStaminaCostSkillType);
        }

        private void ApplyLowHPTemporaryHPEffect(uint defender, int damage)
        {
            var threshold = _stat.GetStatAdjustment(defender, StatType.LowHPTemporaryHPThresholdPercent);
            var temporaryHPPercent = _stat.GetStatAdjustment(defender, StatType.LowHPTemporaryHPPercent);
            var duration = _stat.GetStatAdjustment(defender, StatType.LowHPTemporaryHPDurationSeconds);
            var cooldown = _stat.GetStatAdjustment(defender, StatType.LowHPTemporaryHPCooldownSeconds);

            if (threshold <= 0 ||
                temporaryHPPercent <= 0 ||
                duration <= 0 ||
                !_resources.DidCrossHPThreshold(defender, damage, threshold) ||
                !_resources.TryUseStatTrigger(defender, StatType.LowHPTemporaryHPPercent, cooldown))
                return;

            var temporaryHP = GameMath.PercentOf(GetMaxHitPoints(defender), temporaryHPPercent);
            using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(defender, StatType.LowHPTemporaryHPPercent);
            TemporaryHitPointEffects.ApplyFlat(defender, "LOW_HP_SHIELD", temporaryHP, duration);
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

        private void ApplyLowHPNoSaveTemporaryHPEffect(uint defender, int damage)
        {
            var threshold = _stat.GetStatAdjustment(defender, StatType.LowHPNoSaveTemporaryHPThresholdPercent);
            var temporaryHPPercent = _stat.GetStatAdjustment(defender, StatType.LowHPNoSaveTemporaryHPPercent);
            var duration = _stat.GetStatAdjustment(defender, StatType.LowHPNoSaveTemporaryHPDurationSeconds);
            var cooldown = _stat.GetStatAdjustment(defender, StatType.LowHPNoSaveTemporaryHPCooldownSeconds);

            if (threshold <= 0 ||
                temporaryHPPercent <= 0 ||
                duration <= 0 ||
                !_resources.DidCrossHPThreshold(defender, damage, threshold) ||
                !_resources.TryUseStatTrigger(defender, StatType.LowHPNoSaveTemporaryHPPercent, cooldown))
                return;

            var temporaryHP = GameMath.PercentOf(GetMaxHitPoints(defender), temporaryHPPercent);
            using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(defender, StatType.LowHPNoSaveTemporaryHPPercent);
            TemporaryHitPointEffects.ApplyFlat(defender, "LOW_HP_SHIELD_NO_SAVE", temporaryHP, duration);
        }

        private void ApplyLowHPGuardEffect(uint defender, int damage)
        {
            ApplyLowHPGuardEffect(defender, damage, defender);
        }

        public void ApplyLowHPGuardEffectFromProtectedTarget(uint guardRecipient, uint protectedTarget, int damage)
        {
            ApplyLowHPGuardEffect(protectedTarget, damage, guardRecipient);
        }

        private void ApplyLowHPGuardEffect(uint thresholdCreature, int damage, uint guardRecipient)
        {
            if (!GetIsObjectValid(thresholdCreature) || !GetIsObjectValid(guardRecipient))
                return;

            var threshold = _stat.GetStatAdjustment(guardRecipient, StatType.LowHPGuardThresholdPercent);
            var guardChance = _stat.GetStatAdjustment(guardRecipient, StatType.LowHPGuard);
            var duration = _stat.GetStatAdjustment(guardRecipient, StatType.LowHPGuardDurationSeconds);
            var cooldown = _stat.GetStatAdjustment(guardRecipient, StatType.LowHPGuardCooldownSeconds);

            if (threshold <= 0 ||
                guardChance <= 0 ||
                duration <= 0 ||
                !_resources.DidCrossHPThreshold(thresholdCreature, damage, threshold) ||
                !_resources.TryUseStatTrigger(guardRecipient, StatType.LowHPGuard, cooldown))
                return;

            _statusEffect.ApplyStatusEffect(
                guardRecipient,
                guardRecipient,
                new GuardianReflexesStatusEffect(guardChance),
                duration);

            if (GetIsPC(guardRecipient))
                FloatingTextStringOnCreature(ColorToken.Combat("Guardian Reflexes"), guardRecipient, false);

            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), guardRecipient);
        }

        public void TrackGuardedHit(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            _recentGuardedHits[creature] = Clock.UtcNow;
            _abilityHit.ApplyGuardedHitNextAttackEffects(creature);
            ApplyGuardedHitNextSkillAbilityEffects(creature);
            ApplyGuardedHitNextSkillAbilityStatusEffects(creature);
        }

        public void TrackAvoidedAttack(uint creature, uint attacker)
        {
            if (!GetIsObjectValid(creature))
                return;

            _combatActivity.TrackHostileDefensiveCombatEntryActivity(creature, attacker);

            foreach (var source in _stat.GetStatSources(creature, StatType.AvoidedAttackNextSkillAbilityStaminaCostAdjustment))
            {
                _attackTiming.GrantCuedNextSkillAbilityStaminaCostAdjustment(
                    creature,
                    GetSkillTypeFromStat(source[StatType.AvoidedAttackNextSkillAbilitySkillType]),
                    source[StatType.AvoidedAttackNextSkillAbilityStaminaCostAdjustment],
                    source[StatType.AvoidedAttackNextSkillAbilityWindowSeconds],
                    source.GetModifierGroup(StatType.AvoidedAttackNextSkillAbilityStaminaCostAdjustment),
                    rangedOnly: source[StatType.AvoidedAttackNextSkillAbilityRangedOnly] > 0,
                    hostileOnly: true);
            }

            foreach (var source in _stat.GetStatSources(creature, StatType.AvoidedAttackNextSkillAbilityDamageBonus))
            {
                _attackTiming.GrantNextSkillAbilityBonuses(creature,
                    GetSkillTypeFromStat(source[StatType.AvoidedAttackNextSkillAbilitySkillType]),
                    source[StatType.AvoidedAttackNextSkillAbilityDamageBonus], 0,
                    source[StatType.AvoidedAttackNextSkillAbilityWindowSeconds]);
            }

            var chance = _stat.GetStatAdjustment(creature, StatType.AvoidedAttackStaminaRestoreChance);
            var staminaRestore = _stat.GetStatAdjustment(creature, StatType.AvoidedAttackStaminaRestore);
            var staminaRestoreCooldown = _stat.GetStatAdjustment(creature, StatType.AvoidedAttackStaminaRestoreCooldownSeconds);
            if (chance > 0 &&
                staminaRestore > 0 &&
                Random.D100(1) <= chance &&
                _resources.TryUseStatTrigger(creature, StatType.AvoidedAttackStaminaRestore, staminaRestoreCooldown))
            {
                _resources.RestoreStaminaFromStat(creature, StatType.AvoidedAttackStaminaRestore, staminaRestore);
            }

            var singleChance = _stat.GetStatAdjustment(creature, StatType.AvoidedAttackSingleStaminaRestoreChance);
            var singleStaminaRestore = _stat.GetStatAdjustment(creature, StatType.AvoidedAttackSingleStaminaRestore);
            if (singleChance > 0 && singleStaminaRestore > 0 && Random.D100(1) <= singleChance)
            {
                _statusEffect.ConsumeStatusEffectStat(creature, StatType.AvoidedAttackSingleStaminaRestore);
                _resources.RestoreStaminaFromStat(creature, StatType.AvoidedAttackSingleStaminaRestore, singleStaminaRestore);
            }

            ApplyAvoidedAttackAbilityUsedRangedDeflectionRefresh(creature);
            ApplyAvoidedAttackNextAutoAttackNoDelay(creature);
            ApplyAvoidedAttackAccuracy(creature);
        }

        private void ApplyAvoidedAttackAccuracy(uint creature)
        {
            var accuracy = _stat.GetStatAdjustment(creature, StatType.AvoidedAttackAccuracyPercentAdjustment);
            var duration = _stat.GetStatAdjustment(creature, StatType.AvoidedAttackAccuracyDurationSeconds);
            if (accuracy == 0 || duration <= 0)
                return;

            _temporaryStatModifier.Replace(
                creature,
                StatType.AccuracyPercentAdjustment,
                accuracy,
                duration,
                StatType.AvoidedAttackAccuracyPercentAdjustment);
        }

        private void ApplyAvoidedAttackAbilityUsedRangedDeflectionRefresh(uint creature)
        {
            var duration = _stat.GetStatAdjustment(
                creature,
                StatType.AvoidedAttackAbilityUsedRangedDeflectionRefreshDurationSeconds);
            if (duration <= 0)
                return;

            var deflection = _stat.GetStatAdjustment(
                creature,
                StatType.AbilityUsedRangedDeflection);
            var statusEffectIcon = GetEffectIconTypeFromStat(_stat.GetStatAdjustment(
                creature,
                StatType.AbilityUsedRangedDeflectionStatusEffectIcon));
            if (deflection <= 0 || statusEffectIcon == EffectIconType.Invalid)
                return;

            if (_statusEffect.ApplyStatusEffect(
                    creature,
                    creature,
                    new RangedDeflectionStatusEffect(deflection, statusEffectIcon),
                    duration))
            {
                var source = _stat.GetStatTypeDeflectionSource(StatType.AbilityUsedRangedDeflection);
                ApplyAbilityGrantedAttackDeflectionEffects(creature, source);
            }
        }

        private void ApplyAvoidedAttackNextAutoAttackNoDelay(uint creature)
        {
            var skillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                creature,
                StatType.AvoidedAttackNextAutoAttackNoDelaySkillType));
            var duration = _stat.GetStatAdjustment(
                creature,
                StatType.AvoidedAttackNextAutoAttackNoDelayDurationSeconds);

            if (skillType == SkillType.Invalid)
                _attackTiming.GrantNextAutoAttackNoDelay(creature, duration);
            else
                _attackTiming.GrantNextAutoAttackNoDelay(creature, skillType, duration);
        }

        public void ApplyMeleeDamageTakenEffects(uint defender, uint attacker)
        {
            if (!GetIsObjectValid(defender) || !GetIsObjectValid(attacker))
                return;

            ApplyMeleeDamageTakenPoisonDamage(defender, attacker);

            var chance = _stat.GetStatAdjustment(defender, StatType.MeleeDamageTakenStaminaRestoreChance);
            if (chance <= 0 || Random.D100(1) > chance)
                return;

            var staminaRestore = _stat.GetStatAdjustment(defender, StatType.MeleeDamageTakenStaminaRestore);
            if (staminaRestore > 0)
            {
                _resources.RestoreStaminaFromStat(defender, StatType.MeleeDamageTakenStaminaRestore, staminaRestore);
            }

            var evasion = _stat.GetStatAdjustment(defender, StatType.MeleeDamageTakenEvasionPercentAdjustment);
            var duration = _stat.GetStatAdjustment(defender, StatType.MeleeDamageTakenEvasionDurationSeconds);
            if (evasion != 0 && duration > 0)
            {
                _temporaryStatModifier.Replace(
                    defender,
                    StatType.EvasionPercentAdjustment,
                    evasion,
                    duration,
                    StatType.MeleeDamageTakenEvasionPercentAdjustment);
            }
        }

        private void ApplyMeleeDamageTakenPoisonDamage(uint defender, uint attacker)
        {
            var chance = _stat.GetStatAdjustment(defender, StatType.MeleeDamageTakenPoisonDamageChance);
            var damage = _stat.GetStatAdjustment(defender, StatType.MeleeDamageTakenPoisonDamage);
            if (chance <= 0 || damage <= 0 || Random.D100(1) > chance)
                return;

            var scalingAbility = GetAbilityTypeFromStatPlusOne(
                _stat.GetStatAdjustment(defender, StatType.MeleeDamageTakenPoisonDamageScalingAbility));
            if (scalingAbility != AbilityType.Invalid)
            {
                damage = AbilityEffectScaling.ScaleDirectEffect(
                    damage,
                    GetAbilityScore(defender, scalingAbility),
                    source: defender);
            }

            using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(defender, StatType.MeleeDamageTakenPoisonDamage);
            var appliedDamage = _hitEffects.ApplyTriggeredDamage(defender, attacker, damage, CombatDamageType.Poison);
            if (appliedDamage <= 0)
                return;

            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Poison_S), attacker);
        }

        public int ApplyGuardedHitModifiers(
            uint defender,
            uint attacker,
            int damage,
            CombatDamageType damageType,
            bool isLandedAttack)
        {
            if (!isLandedAttack ||
                !GetIsObjectValid(defender) ||
                !GetIsObjectValid(attacker) ||
                defender == attacker ||
                damage <= 0 ||
                !damageType.IsPhysicalDamageType() ||
                !IsHostileAttackSource(defender, attacker))
                return damage;

            var guardChance = _stat.GetGuardChance(defender);
            if (guardChance <= 0 || Random.D100(1) > guardChance)
                return damage;

            var reductionPercent = GetGuardDamageReductionPercent(defender);
            var preventedDamage = Math.Min(damage, GameMath.PercentOf(damage, reductionPercent));
            var adjustedDamage = Math.Max(0, damage - preventedDamage);

            TrackGuardedHit(defender);
            _statusEffect.OnGuardedHit(defender, attacker, preventedDamage);
            ApplyGuardedHitRecovery(defender);
            ApplyGuardedHitRetaliation(attacker, defender, damage);
            ApplyGuardedHitEnmity(attacker, defender, damage);
            _feedback.SendGuardedHitFeedback(defender, attacker, preventedDamage);

            return adjustedDamage;
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

        private void ApplyGuardedHitRecovery(uint defender)
        {
            var staminaRestore = _stat.GetStatAdjustment(defender, StatType.GuardStaminaRestore);
            var cooldown = _stat.GetStatAdjustment(defender, StatType.GuardStaminaRestoreCooldownSeconds);
            if (staminaRestore > 0 &&
                _resources.TryUseStatTrigger(defender, StatType.GuardStaminaRestore, cooldown))
            {
                _resources.RestoreStaminaFromStat(defender, StatType.GuardStaminaRestore, staminaRestore);
            }
        }

        private void ApplyGuardedHitRetaliation(uint attacker, uint defender, int incomingDamage)
        {
            var skillType = _queries.GetEquippedWeaponSkillType(defender);
            var retaliationDMG = _stat.GetStatAdjustment(defender, StatType.GuardRetaliationDMG);
            var pulseDMG = _stat.GetStatAdjustment(defender, StatType.GuardedHitPulseDMG);

            // DMG is an input to the attack-versus-defense damage range, not already-resolved HP damage.
            // Resolve each retaliation before using the non-recursive triggered-damage delivery path.
            if (retaliationDMG > 0)
            {
                var retaliationDamage = ResolveGuardRetaliationDamage(
                    defender,
                    attacker,
                    retaliationDMG,
                    skillType);
                using var retaliationAttribution = _combatRecorder.BeginStatTriggerAttribution(defender, StatType.GuardRetaliationDMG);
                _hitEffects.ApplyTriggeredDamage(defender, attacker, retaliationDamage, CombatDamageType.Physical, skillType);
            }

            if (pulseDMG <= 0)
                return;

            using var pulseAttribution = _combatRecorder.BeginStatTriggerAttribution(defender, StatType.GuardedHitPulseDMG);

            var radius = _stat.GetStatAdjustment(defender, StatType.GuardedHitPulseRadiusMeters);
            if (radius <= 0)
            {
                var pulseDamage = ResolveGuardRetaliationDamage(defender, attacker, pulseDMG, skillType);
                _hitEffects.ApplyTriggeredDamage(defender, attacker, pulseDamage, CombatDamageType.Physical, skillType);
                return;
            }

            ApplyGuardedHitRetaliationPulse(defender, attacker, incomingDamage, pulseDMG, radius, skillType);
        }

        private void ApplyGuardedHitRetaliationPulse(
            uint defender,
            uint originalAttacker,
            int incomingDamage,
            int dmg,
            float radius,
            SkillType skillType)
        {
            var enmityPercent = _stat.GetStatAdjustment(
                defender,
                StatType.GuardedHitPulseEnmityPercentOfIncomingDamage);
            var additionalTargetEnmity = enmityPercent > 0
                ? Math.Max(1, GameMath.PercentOf(incomingDamage, enmityPercent))
                : 0;
            var location = GetLocation(defender);
            var applied = false;

            // The creature whose hit was guarded is always affected, even when attacking from
            // beyond the nearby-enemy radius. Normal Guard handling supplies this target's _enmity.
            if (GetIsObjectValid(originalAttacker) &&
                !GetIsDead(originalAttacker) &&
                GetCurrentHitPoints(originalAttacker) > 0 &&
                GetIsReactionTypeHostile(originalAttacker, defender))
            {
                var damage = ResolveGuardRetaliationDamage(defender, originalAttacker, dmg, skillType);
                _hitEffects.ApplyTriggeredDamage(defender, originalAttacker, damage, CombatDamageType.Physical, skillType);
                applied = true;
            }

            foreach (var target in ObjectSnapshot.InShape(Shape.Sphere, radius, location, true, SWLOR.NWN.API.NWScript.Enum.ObjectType.Creature))
            {
                if (!GetIsObjectValid(target))
                    continue;

                if (target != originalAttacker &&
                    !GetIsDead(target) &&
                    GetCurrentHitPoints(target) > 0 &&
                    GetIsReactionTypeHostile(target, defender))
                {
                    var damage = ResolveGuardRetaliationDamage(defender, target, dmg, skillType);
                    _hitEffects.ApplyTriggeredDamage(defender, target, damage, CombatDamageType.Physical, skillType);
                    if (additionalTargetEnmity > 0)
                        _enmity.ModifyEnmity(defender, target, additionalTargetEnmity);

                    applied = true;
                }

            }

            if (applied && GetIsPC(defender))
                FloatingTextStringOnCreature(ColorToken.Combat("Retaliation Pulse"), defender, false);
        }

        private int ResolveGuardRetaliationDamage(
            uint source,
            uint target,
            int dmg,
            SkillType skillType)
        {
            if (!GetIsObjectValid(source) || !GetIsObjectValid(target) || dmg <= 0)
                return 0;

            const CombatDamageType damageType = CombatDamageType.Physical;
            var damageAbility = GetGuardRetaliationDamageAbility(source, skillType);
            var attack = _stat.GetAttack(source, damageAbility, skillType);
            attack = _damageModifiers.ApplyTargetStatusAttackModifiers(source, target, attack, skillType);
            var attackStat = GetAbilityScore(source, damageAbility);
            var defenseAbility = damageType.GetDefenseAbilityType();
            var defense = _stat.GetDefense(target, damageType, defenseAbility);
            defense = _damageModifiers.ApplyStatusSourceDefenseModifiers(source, target, defense);
            defense = ApplyIncomingPhysicalToForceDefenseConversion(
                target,
                damageType,
                defense,
                () => _damageModifiers.ApplyStatusSourceDefenseModifiers(
                    source,
                    target,
                    _stat.GetDefense(target, CombatDamageType.Force, CombatDamageType.Force.GetDefenseAbilityType())));
            var defenderStat = GetAbilityScore(target, defenseAbility);

            return CalculateDamage(
                attack,
                dmg,
                attackStat,
                defense,
                defenderStat,
                0);
        }

        private AbilityType GetGuardRetaliationDamageAbility(uint defender, SkillType skillType)
        {
            var weapon = _queries.GetRelevantSkillWeapon(defender, skillType);
            if (!GetIsObjectValid(weapon))
                return AbilityType.Might;

            var ability = _queries.GetWeaponDamageAbilityType(defender, GetBaseItemType(weapon));
            return ability == AbilityType.Invalid
                ? AbilityType.Might
                : ability;
        }

        private void ApplyGuardedHitEnmity(uint attacker, uint defender, int damage)
        {
            var enmity = Math.Max(1, damage);
            var percentAdjustment = _stat.GetStatAdjustment(defender, StatType.GuardEnmityPercentAdjustment);
            if (percentAdjustment != 0)
            {
                enmity = Math.Max(1, (int)Math.Ceiling(enmity * ((100 + percentAdjustment) / 100f)));
            }

            _enmity.ModifyEnmity(defender, attacker, enmity);
        }

        public bool HasRecentGuardedHit(uint creature, float windowSeconds)
        {
            if (!GetIsObjectValid(creature) || windowSeconds <= 0f)
                return false;

            if (!_recentGuardedHits.TryGetValue(creature, out var lastGuardedHit))
                return false;

            var isRecent = (Clock.UtcNow - lastGuardedHit).TotalSeconds <= windowSeconds;
            if (!isRecent)
                _recentGuardedHits.Remove(creature);

            return isRecent;
        }

        public void TrackDeflection(uint creature, DeflectionSource source)
        {
            if (!GetIsObjectValid(creature) || source == DeflectionSource.None)
                return;

            _recentDeflections[(creature, source)] = Clock.UtcNow;
            ApplyDeflectionNearbyAllyGuard(creature, source);
        }

        private void ApplyDeflectionNearbyAllyGuard(uint creature, DeflectionSource source)
        {
            if (_stat.GetStatTypeDeflectionSource(StatType.DeflectionNearbyAllyGuard) != source)
                return;

            var guard = _stat.GetStatAdjustment(creature, StatType.DeflectionNearbyAllyGuard);
            var duration = _stat.GetStatAdjustment(creature, StatType.DeflectionNearbyAllyGuardDurationSeconds);
            if (guard <= 0 || duration <= 0)
                return;

            var ally = AbilityTargeting.GetFriendlyTargetsNearLocation(creature, GetLocation(creature), 5f, false)
                .Where(target => target != creature)
                .OrderBy(target => GetDistanceBetween(creature, target))
                .FirstOrDefault();
            if (!GetIsObjectValid(ally))
                return;

            _temporaryStatModifier.Replace(
                ally,
                StatType.Guard,
                guard,
                duration,
                StatType.DeflectionNearbyAllyGuard);
        }

        public bool HasRecentDeflection(uint creature, DeflectionSource source, float windowSeconds)
        {
            if (!GetIsObjectValid(creature) || source == DeflectionSource.None || windowSeconds <= 0f)
                return false;

            var key = (creature, source);
            if (!_recentDeflections.TryGetValue(key, out var lastDeflection))
                return false;

            var isRecent = (Clock.UtcNow - lastDeflection).TotalSeconds <= windowSeconds;
            if (!isRecent)
                _recentDeflections.Remove(key);

            return isRecent;
        }

        private void ApplyGuardedHitNextSkillAbilityEffects(uint creature)
        {
            var primary = GetGuardedHitNextSkillAbilityBonuses(
                creature,
                StatType.GuardedHitNextSkillAbilitySkillType,
                StatType.GuardedHitNextSkillAbilityDamageBonus,
                StatType.GuardedHitNextSkillAbilityCriticalRatePercentAdjustment,
                StatType.GuardedHitNextSkillAbilityWindowSeconds);
            var secondary = GetGuardedHitNextSkillAbilityBonuses(
                creature,
                StatType.GuardedHitSecondaryNextSkillAbilitySkillType,
                StatType.GuardedHitSecondaryNextSkillAbilityDamageBonus,
                StatType.GuardedHitSecondaryNextSkillAbilityCriticalRatePercentAdjustment,
                StatType.GuardedHitSecondaryNextSkillAbilityWindowSeconds);

            var selected = primary;
            if (primary.SkillType == SkillType.Invalid)
            {
                selected = secondary;
            }
            // Guarded-hit bonuses sharing a skill are one window and intentionally stack their payloads.
            // Keeping their selector/window providers independent prevents integer selector values and
            // durations from being accidentally added by _stat.GetStatAdjustment.
            else if (secondary.SkillType == SkillType.Invalid || secondary.SkillType == primary.SkillType)
            {
                selected = (
                    primary.SkillType,
                    primary.DamageBonus + secondary.DamageBonus,
                    primary.CriticalRate + secondary.CriticalRate,
                    Math.Max(primary.Window, secondary.Window));
            }
            else
            {
                // Different weapon selectors cannot both be consumed by one "next skill ability" slot.
                // Prefer the currently equipped weapon's channel; otherwise preserve the primary channel.
                var equippedSkillType = _queries.GetEquippedWeaponSkillType(creature);
                if (secondary.SkillType == equippedSkillType)
                    selected = secondary;
            }

            _attackTiming.GrantNextSkillAbilityBonuses(
                creature,
                selected.SkillType,
                selected.DamageBonus,
                selected.CriticalRate,
                selected.Window);

            if (selected.SkillType != SkillType.Invalid &&
                selected.Window > 0 &&
                (selected.DamageBonus != 0 || selected.CriticalRate != 0) &&
                GetIsPC(creature))
            {
                var criticalText = selected.CriticalRate != 0
                    ? $", +{selected.CriticalRate}% Crit"
                    : string.Empty;
                FloatingTextStringOnCreature(
                    ColorToken.Combat($"Counter Ready: +{selected.DamageBonus} DMG{criticalText}"),
                    creature,
                    false);
            }
        }

        private (SkillType SkillType, int DamageBonus, int CriticalRate, int Window)
            GetGuardedHitNextSkillAbilityBonuses(
            uint creature,
            StatType skillTypeStat,
            StatType damageBonusStat,
            StatType criticalRateStat,
            StatType windowStat)
        {
            var skillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(creature, skillTypeStat));
            var criticalRate = _stat.GetStatAdjustment(creature, criticalRateStat);
            var damageBonus = _stat.GetStatAdjustment(creature, damageBonusStat);
            var window = _stat.GetStatAdjustment(creature, windowStat);
            return (skillType, damageBonus, criticalRate, window);
        }

        private void ApplyGuardedHitNextSkillAbilityStatusEffects(uint creature)
        {
            var skillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                creature,
                StatType.GuardedHitNextSkillAbilityStatusSkillType));
            var duration = _stat.GetStatAdjustment(creature, StatType.GuardedHitNextSkillAbilityExposedDurationSeconds);
            var damageBonus = _stat.GetStatAdjustment(creature, StatType.GuardedHitNextSkillAbilityExposedDamageBonus);
            if (skillType == SkillType.Invalid || duration <= 0 && damageBonus <= 0)
                return;

            var wasArmed = _temporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.GuardedHitNextSkillAbilityExposedDamageBonus,
                StatType.GuardedHitNextSkillAbilityExposedDurationSeconds) != 0;
            _attackTiming.ShowReadyCue(creature, wasArmed, damageBonus, ReadyCue.Damage("Guard Reversal", damageBonus));

            var window = Math.Max(1, duration);
            _temporaryStatModifier.Replace(
                creature,
                StatType.GuardedHitNextSkillAbilityStatusSkillType,
                (int)skillType,
                window,
                StatType.GuardedHitNextSkillAbilityExposedDurationSeconds);

            if (duration > 0)
            {
                _temporaryStatModifier.Replace(
                    creature,
                    StatType.GuardedHitNextSkillAbilityExposedDurationSeconds,
                    duration,
                    window,
                    StatType.GuardedHitNextSkillAbilityExposedDurationSeconds);
            }

            if (damageBonus > 0)
            {
                _temporaryStatModifier.Replace(
                    creature,
                    StatType.GuardedHitNextSkillAbilityExposedDamageBonus,
                    damageBonus,
                    window,
                    StatType.GuardedHitNextSkillAbilityExposedDurationSeconds);
            }
        }

        public int ConsumeGuardedHitNextSkillAbilityExposedDamageBonus(uint creature, SkillType skillType)
        {
            var storedSkillType = GetSkillTypeFromStat(_temporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.GuardedHitNextSkillAbilityStatusSkillType,
                StatType.GuardedHitNextSkillAbilityExposedDurationSeconds));
            if (!SkillTypeMatches(skillType, storedSkillType))
                return 0;

            return _temporaryStatModifier.Consume(
                creature,
                StatType.GuardedHitNextSkillAbilityExposedDamageBonus,
                StatType.GuardedHitNextSkillAbilityExposedDurationSeconds);
        }

        private void ApplyRecentDamageTargetHitEffects(uint defender, uint attacker)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(defender) || attacker == defender)
                return;

            var chance = _stat.GetStatAdjustment(defender, StatType.DamageTakenRecentTargetNextAbilityNoDelayChance);
            var window = _stat.GetStatAdjustment(defender, StatType.DamageTakenRecentTargetWindowSeconds);
            var skillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(defender, StatType.DamageTakenRecentTargetNextAbilityNoDelaySkillType));
            if (chance <= 0 || window <= 0 || skillType == SkillType.Invalid)
                return;

            if (!_combatActivity.RecentDamageTargets.TryGetValue((defender, attacker), out var lastDamaged) ||
                (Clock.UtcNow - lastDamaged).TotalSeconds > window ||
                Random.D100(1) > chance)
                return;

            _attackTiming.GrantNextAbilityNoDelay(defender, skillType, window);
        }

        /// <summary>
        /// First half of Saber Ward / Aegis Eternal conversion: give the converted share its Force Defense
        /// mitigation. The physical hit's damage roll uses a defense value blended between the defender's
        /// Physical Defense and Force Defense by the conversion percent, so the converted share is mitigated
        /// as Force. The single damage roll is RNG-based and consumes stateful modifiers, so it cannot be run
        /// twice per hit; blending the defense is how the Force-Defense mitigation is applied without a second
        /// roll. The re-typing itself — removing the converted share from the physical hit and dealing it as a
        /// real Force instance (Force resistance, combat-log visibility) — is done afterward by
        /// <see cref="ApplyIncomingPhysicalToForceConversion"/>. The percentage is read from
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

        /// <summary>
        /// Saber Ward (and Aegis Eternal) re-type a percentage of an incoming physical hit into a real
        /// Force damage instance. The converted share's Force Defense mitigation is already reflected in
        /// <paramref name="physicalDamage"/> because <see cref="ApplyIncomingPhysicalToForceDefenseConversion"/>
        /// blends the defense used for the damage roll toward Force Defense by the same percent. This removes
        /// the converted share from <paramref name="physicalDamage"/> (so it is not also dealt as physical) and
        /// deals it as Force damage, so it is reduced by the defender's Force resistance and shown as Force in
        /// the combat log. Runs before the physical-resistance stage; the Force portion is routed through
        /// <see cref="ApplyTriggeredDamage"/>, which applies Force resistance, the Force-specific Leadership
        /// channel, and the remaining damage-taken pipeline.
        /// Returns the pre-resistance Force amount split off (0 when nothing converts).
        /// </summary>
        public int ApplyIncomingPhysicalToForceConversion(
            uint attacker,
            uint defender,
            CombatDamageType damageType,
            ref int physicalDamage,
            int targetStatusDamagePercentAdjustment = 0,
            Action<Action> convertedDamageScheduler = null)
        {
            if (physicalDamage <= 0 || !damageType.IsPhysicalDamageType())
                return 0;

            var conversionPercent = _stat.GetStatAdjustment(defender, StatType.IncomingPhysicalToForceConversionPercent);
            var forcePortion = GetIncomingPhysicalToForceConversionPortion(physicalDamage, conversionPercent);
            if (forcePortion <= 0)
                return 0;

            physicalDamage -= forcePortion;

            // This method runs inside the native GetDamageRoll hook and the ability damage paths, i.e. while
            // the current attack's own damage has not yet resolved. Applying a fully dispatched damage event
            // (ApplyTriggeredDamage) synchronously here re-enters the engine's OnCreatureDamaged / AI / enmity
            // chain mid-attack and clobbers shared combat state (GetLastDamager / OBJECT_SELF), which caused
            // runaway reflect cascades and mis-targeted reflects with effects such as Blazing Spikes. Defer the
            // Force portion to the next frame so it lands as a clean, separate damage instance off the attack's
            // stack; the physical carve above stays synchronous so the physical hit is correctly reduced.
            var recorderAttribution = _combatRecorder.CaptureAttackAttribution(attacker);
            void ApplyConvertedDamage()
            {
                using var recorderScope = _combatRecorder.BeginAttribution(recorderAttribution);
                if (GetIsObjectValid(attacker) && GetIsObjectValid(defender))
                    _hitEffects.ApplyTriggeredDamage(
                        attacker,
                        defender,
                        forcePortion,
                        CombatDamageType.Force,
                        typedLeadershipReductionAlreadyApplied: false,
                        targetStatusDamagePercentAdjustment: targetStatusDamagePercentAdjustment,
                        outgoingModifiersAlreadyApplied: true);
            }
            // Projectile callers retain the entire converted hit, including its reactions and
            // attribution, until arrival. Native swings and ability impacts keep next-frame dispatch.
            // Native swings resolve in a script context with no object, where a bare DelayCommand has no
            // OBJECT_SELF to own it and is silently dropped; module ownership keeps the Force hit queued.
            if (convertedDamageScheduler != null) convertedDamageScheduler(ApplyConvertedDamage);
            else AssignCommand(GetModule(), () => DelayCommand(0.0f, ApplyConvertedDamage));
            return forcePortion;
        }

        /// <summary>
        /// Deflecting Return: when the defender deflects a directly targeted ranged attack, reflect a capped
        /// share of weapon damage back to the attacker as Force damage. Fires at most once every
        /// <see cref="DeflectingReturnCooldownSeconds"/> seconds. Reflection amount is driven by the
        /// <see cref="StatType.RangedDeflectionReflectionPercent"/> / <see cref="StatType.RangedDeflectionReflectionCapPercent"/>
        /// stats the Deflecting Return perk grants. Embattled high-stack bonuses and finite capstone
        /// overrides are also stat-driven.
        /// </summary>
        public int ApplyRangedDeflectionReflection(uint defender, uint attacker, SkillType attackerWeaponSkill)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(defender))
                return 0;

            var (reflectPercent, capPercent) = GetRangedDeflectionReflectionRates(
                _stat.GetStatAdjustment(defender, StatType.RangedDeflectionReflectionPercent),
                _stat.GetStatAdjustment(defender, StatType.RangedDeflectionReflectionCapPercent),
                EmbattledStatusEffect.GetStackCount(defender),
                _stat.GetStatAdjustment(defender, StatType.EmbattledHighStackThreshold),
                _stat.GetStatAdjustment(defender, StatType.EmbattledHighStackDeflectionReflectionBonusPercent),
                _stat.GetStatAdjustment(defender, StatType.RangedDeflectionReflectionOverridePercent),
                _stat.GetStatAdjustment(defender, StatType.RangedDeflectionReflectionCapOverridePercent));
            if (reflectPercent <= 0)
                return 0;

            var reflected = GetRangedDeflectionReflectionAmount(
                _queries.GetCombatImpactWeaponDamage(attacker, attackerWeaponSkill, requireMatchingSkill: true),
                reflectPercent,
                _queries.GetCombatImpactWeaponDamage(defender, _queries.GetEquippedWeaponSkillType(defender)),
                capPercent);
            if (reflected <= 0)
                return 0;

            // Consume the shared cooldown only when a hit will actually be reflected.
            if (!_resources.TryUseStatTrigger(defender, StatType.RangedDeflectionReflectionPercent, DeflectingReturnCooldownSeconds))
                return 0;

            using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(defender, StatType.RangedDeflectionReflectionPercent);
            var appliedDamage = _hitEffects.ApplyTriggeredDamage(defender, attacker, reflected, CombatDamageType.Force);
            if (appliedDamage <= 0)
                return 0;

            _messaging.SendMessageNearbyToPlayers(
                defender,
                observer => BuildDeflectingReturnCombatLogMessage(observer, defender, attacker, appliedDamage),
                60f);
            return appliedDamage;
        }

        public string BuildDeflectingReturnCombatLogMessage(
            uint observer,
            uint defender,
            uint attacker,
            int damage)
        {
            var defenderName = _playerName.GetColoredDisplayName(observer, defender);
            var attackerName = _playerName.GetColoredDisplayName(observer, attacker);
            return ColorToken.Combat($"{defenderName}'s Deflecting Return reflects {damage} Force damage to {attackerName}.");
        }

        public void ApplyAbilityUsedAttackDeflection(
            uint activator,
            StatType attackDeflectionStatType,
            StatType durationStatType,
            StatType deflectionFPRestoreStatType = StatType.Invalid)
        {
            var attackDeflection = _stat.GetStatAdjustment(activator, attackDeflectionStatType);
            var duration = _stat.GetStatAdjustment(activator, durationStatType);
            if (attackDeflection == 0 || duration <= 0)
                return;

            var source = _stat.GetStatTypeDeflectionSource(attackDeflectionStatType);
            var targetStatType = _stat.GetGrantedDeflectionStatType(attackDeflectionStatType);
            if (targetStatType == StatType.Invalid)
                return;

            _temporaryStatModifier.Replace(
                activator,
                targetStatType,
                attackDeflection,
                duration,
                attackDeflectionStatType);
            if (deflectionFPRestoreStatType != StatType.Invalid)
            {
                var fpRestore = _stat.GetStatAdjustment(activator, deflectionFPRestoreStatType);
                var fpRestoreSource = _stat.GetStatTypeDeflectionSource(deflectionFPRestoreStatType);
                if (fpRestore > 0 && fpRestoreSource == source)
                {
                    var fpRestoreTargetStatType = fpRestoreSource switch
                    {
                        DeflectionSource.Melee => StatType.MeleeDeflectionFPRestore,
                        DeflectionSource.Ranged => StatType.DeflectionFPRestore,
                        _ => StatType.Invalid
                    };
                    if (fpRestoreTargetStatType != StatType.Invalid)
                    {
                        _temporaryStatModifier.Replace(
                            activator,
                            fpRestoreTargetStatType,
                            fpRestore,
                            duration,
                            deflectionFPRestoreStatType);
                    }
                }
            }

            if (source == DeflectionSource.Ranged)
                ApplyAbilityGrantedAttackDeflectionEffects(activator, source);
        }

        public void ApplyAbilityGrantedAttackDeflectionEffects(uint activator, DeflectionSource source)
        {
            if (_stat.GetStatTypeDeflectionSource(StatType.AbilityGrantedAttackDeflectionFPRestore) != source)
                return;

            var fpRestore = _stat.GetStatAdjustment(activator, StatType.AbilityGrantedAttackDeflectionFPRestore);
            var cooldown = _stat.GetStatAdjustment(activator, StatType.AbilityGrantedAttackDeflectionFPRestoreCooldownSeconds);
            if (fpRestore <= 0 || !_resources.TryUseStatTrigger(activator, StatType.AbilityGrantedAttackDeflectionFPRestore, cooldown))
                return;

            using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(
                activator, StatType.AbilityGrantedAttackDeflectionFPRestore);
            if (_stat.RestoreFP(activator, fpRestore) > 0)
                _resources.ApplyAbilityRestoredFPEffects(activator);
        }

        public void ForgetDamageTakenState(uint creature)
        {
            _recentGuardedHits.Remove(creature);
            foreach (var key in _recentDeflections.Keys.Where(x => x.Creature == creature).ToList())
            {
                _recentDeflections.Remove(key);
            }
        }
    }
}
