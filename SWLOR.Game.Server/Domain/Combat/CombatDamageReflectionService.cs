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
    /// The damage given back part of <c>Combat</c>: the reflection of damage taken, the Force defense gained from Force
    /// damage taken, the incoming physical-to-Force conversion and the Deflecting Return reflection of a ranged deflection.
    /// </summary>
    public class CombatDamageReflectionService : ICombatDamageReflectionService
    {
        private readonly ICombatQueriesService _queries;
        private readonly ICombatResourcesService _resources;
        private readonly ICombatHitEffectsService _hitEffects;
        private readonly IStatService _stat;
        private readonly IStatusEffectApplicationService _statusEffectApplication;
        private readonly ICombatRecorderService _combatRecorder;
        private readonly IMessagingService _messaging;
        private readonly IPlayerNameService _playerName;

        public CombatDamageReflectionService(
            ICombatQueriesService queries,
            ICombatResourcesService resources,
            ICombatHitEffectsService hitEffects,
            IStatService stat,
            IStatusEffectApplicationService statusEffectApplication,
            ICombatRecorderService combatRecorder,
            IMessagingService messaging,
            IPlayerNameService playerName)
        {
            _queries = queries;
            _resources = resources;
            _hitEffects = hitEffects;
            _stat = stat;
            _statusEffectApplication = statusEffectApplication;
            _combatRecorder = combatRecorder;
            _messaging = messaging;
            _playerName = playerName;
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

            _statusEffectApplication.ApplyStatusEffect(
                defender,
                defender,
                new ForceWardingStatusEffect(forceDefense),
                durationSeconds,
                CombatDamageType.Force);
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
    }
}
