using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// Status effect durations (part 4 of the status effect split): the timing of an application (the outgoing adjustments, the
    /// resistance, immunity and control budget), the refresh in place, the restart, the extension and the refresh of an active
    /// effect. The trackers, the removal and the native effect stay in <see cref="StatusEffectService"/>; this part calls them
    /// through <see cref="IStatusEffectService"/>. The stat read and the active ability impact sit behind one-line
    /// <c>protected virtual</c> methods whose bodies are the original calls.
    /// </summary>
    public class StatusEffectDurationService : IStatusEffectDurationService
    {
        private readonly IStatusEffectService _statusEffect;
        private readonly IStatusResistance _resistance;
        private readonly ICharacterRecording _combatRecorder;
        private readonly Lazy<IStatService> _stat;
        private readonly IActiveAbilityImpact _activeImpact;

        public StatusEffectDurationService(
            IStatusEffectService statusEffect,
            IStatusResistance resistance,
            ICharacterRecording combatRecorder,
            Lazy<IStatService> stat,
            IActiveAbilityImpact activeImpact)
        {
            _statusEffect = statusEffect;
            _resistance = resistance;
            _combatRecorder = combatRecorder;
            _stat = stat;
            _activeImpact = activeImpact;
        }

        /// <summary>
        /// Works out how long an application lasts: the outgoing adjustments, the resistance, immunity and control budget. False
        /// (with the "resisted" message sent to the source) when the target is immune or nothing is left of the duration.
        /// </summary>
        public bool ResolveApplicationTiming(
            IStatusEffect statusEffect,
            uint source,
            uint creature,
            float durationSeconds,
            bool isPermanent,
            ResistanceType resistanceOverride,
            CombatDamageType sourceDamageType,
            out StatusApplicationTiming timing)
        {
            timing = default;
            durationSeconds = ApplyOutgoingStatusDurationAdjustments(statusEffect, source, durationSeconds, isPermanent);
            var frequency = Math.Max(1f, statusEffect.Frequency);
            var durationTicks = isPermanent ? -1 : Math.Max(1, (int)Math.Ceiling(durationSeconds / frequency));
            ApplyOutgoingStatusStatAdjustments(statusEffect, source);

            var resistanceType = ResolveResistanceType(statusEffect, resistanceOverride, sourceDamageType);
            var durationTicksWithoutResistance = durationTicks;
            if (!isPermanent &&
                durationTicks > 0 &&
                GetIsObjectValid(source) &&
                GetIsObjectValid(creature) &&
                GetIsReactionTypeHostile(creature, source))
            {
                if (_resistance.IsValidResistanceType(resistanceType))
                {
                    if (_resistance.HasImmunity(creature, resistanceType))
                    {
                        SendMessageToPC(source, "Your ability was resisted.");
                        return false;
                    }

                    durationTicks = _resistance.CalculateResistedTicks(creature, resistanceType, durationTicks);
                }
            }

            if (!isPermanent)
            {
                var isPlayerAgainstPlayer = PlayerControlRules.IsPlayerAgainstPlayer(source, creature);
                durationTicks = StatusEffectRules.ClampHardCrowdControlDurationTicks(statusEffect.Categories, durationTicks, statusEffect.Frequency,
                    isSelfApplied: source == creature, isPlayerAgainstPlayer: isPlayerAgainstPlayer);
                durationTicksWithoutResistance = StatusEffectRules.ClampHardCrowdControlDurationTicks(
                    statusEffect.Categories, durationTicksWithoutResistance, statusEffect.Frequency,
                    isSelfApplied: source == creature, isPlayerAgainstPlayer: isPlayerAgainstPlayer);
            }

            if (!isPermanent && durationTicks <= 0)
            {
                SendMessageToPC(source, "Your ability was resisted.");
                return false;
            }

            // Apply resistance/control budgets after duration bonuses without rounding a short
            // duration bonus into an entire damage interval.
            if (!isPermanent)
                durationSeconds = Math.Max(0.1f, Math.Min(durationSeconds +
                    Math.Max(0, durationTicks - durationTicksWithoutResistance) * frequency, durationTicks * frequency));

            timing = new StatusApplicationTiming(durationSeconds, frequency, durationTicks, durationTicksWithoutResistance, resistanceType);
            return true;
        }

        /// <summary>
        /// Restarts an opted-in effect in place when applying it again would produce the same state.
        /// A non-stacking effect from another source is handed to the new source, as a fresh replacement
        /// would be. Duration, tick schedule, native effect and application triggers match a fresh
        /// application; the remove/add cycle and the repeated application message are skipped.
        /// </summary>
        public bool TryRefreshInPlace(
            IStatusEffect statusEffect,
            uint source,
            uint creature,
            int durationTicks,
            int durationTicksWithoutResistance,
            float durationSeconds,
            ResistanceType resistanceType)
        {
            if (statusEffect.StackingType is not (StatusEffectStackType.Disabled or StatusEffectStackType.StackFromMultipleSources) ||
                !_statusEffect.TryGetCreatureStatusEffects(creature, out var creatureEffects))
            {
                return false;
            }

            // Cheap opt-in check first; most effects never refresh in place.
            var type = statusEffect.GetType();
            var first = creatureEffects.GetEffect(type);
            if (first == null || !statusEffect.CanRefreshInPlace(first))
                return false;

            // A fresh application replaces every instance of a non-stacking type, so only a lone instance qualifies.
            var candidates = creatureEffects.GetAllEffects()
                .Where(effect => effect.GetType() == type &&
                                 (statusEffect.StackingType == StatusEffectStackType.Disabled || effect.Source == source))
                .ToList();
            if (candidates.Count != 1)
                return false;

            var active = candidates[0];
            var originatingAbility = statusEffect.OriginatingAbility ?? GetActiveAbilityImpactSummary(source)?.Ability;
            if (active.IsFlaggedForRemoval ||
                active.DurationTicks < 0 ||
                active.AppliedResistanceType != resistanceType ||
                active.OriginatingAbility != originatingAbility ||
                statusEffect.StatGroup.Stats.Any(stat =>
                    stat.Value != 0 && active.StatGroup.Stats.GetValueOrDefault(stat.Key) != stat.Value) ||
                !statusEffect.CanRefreshInPlace(active))
            {
                return false;
            }

            _statusEffect.RemoveOtherCommandStatuses(creature, type, source);
            foreach (var lessPowerful in statusEffect.LessPowerfulEffectTypes)
            {
                _statusEffect.RemoveStatusEffectForReplacement(lessPowerful, creature, OBJECT_INVALID);
            }

            var tickAnchor = StatusEffectRules.GetRefreshTickAnchor(statusEffect, creatureEffects.GetAllEffects(), source);
            if (active.Source != source)
                active.ReassignSource(source);
            RestartInPlace(creature, active, source, durationTicks, durationTicksWithoutResistance, durationSeconds,
                tickAnchor, resistanceType);
            return true;
        }

        /// <summary>
        /// Restarts an active effect's duration exactly as re-applying the same effect from the same
        /// source would, without replacing it or repeating its application message. For effects whose
        /// re-application changes nothing else, such as Embattled with unchanged stacks.
        /// </summary>
        public bool RestartStatusEffectDuration(
            uint creature,
            Type statusEffectClass,
            uint source,
            float durationSeconds)
        {
            if (!_statusEffect.TryGetCreatureStatusEffects(creature, out var creatureEffects) ||
                statusEffectClass == null ||
                durationSeconds <= 0f)
            {
                return false;
            }

            var matches = creatureEffects.GetAllEffects()
                .Where(effect => effect.GetType() == statusEffectClass && effect.Source == source)
                .ToList();
            if (matches.Count != 1)
                return false;

            var active = matches[0];
            if (active.IsFlaggedForRemoval ||
                active.DurationTicks < 0 ||
                (active.Categories & StatusEffectCategory.HardCrowdControl) != 0)
            {
                return false;
            }

            var frequency = Math.Max(1f, active.Frequency);
            var adjustedSeconds = ApplyOutgoingStatusDurationAdjustments(active, source, durationSeconds, false);
            var ticks = Math.Max(1, (int)Math.Ceiling(adjustedSeconds / frequency));
            var ticksWithoutResistance = ticks;
            var resistanceType = active.AppliedResistanceType;
            if (_resistance.IsValidResistanceType(resistanceType) &&
                GetIsObjectValid(source) &&
                GetIsObjectValid(creature) &&
                GetIsReactionTypeHostile(creature, source))
            {
                if (_resistance.HasImmunity(creature, resistanceType))
                    return false;

                ticks = _resistance.CalculateResistedTicks(creature, resistanceType, ticks);
            }

            if (ticks <= 0)
                return false;

            adjustedSeconds = Math.Max(0.1f, Math.Min(adjustedSeconds + Math.Max(0, ticks - ticksWithoutResistance) * frequency,
                ticks * frequency));
            var tickAnchor = StatusEffectRules.GetRefreshTickAnchor(active, creatureEffects.GetAllEffects(), source);
            RestartInPlace(creature, active, source, ticks, ticksWithoutResistance, adjustedSeconds, tickAnchor, resistanceType);
            return true;
        }

        private void RestartInPlace(
            uint creature,
            IStatusEffect active,
            uint source,
            int durationTicks,
            int durationTicksWithoutResistance,
            float durationSeconds,
            DateTime? tickAnchor,
            ResistanceType resistanceType)
        {
            active.RestartDuration(durationTicks, durationSeconds, tickAnchor);
            _statusEffect.ReplaceNativeStatusEffect(creature, active, active.DurationTicks, false);
            if (_combatRecorder.IsRecording)
                _combatRecorder.RecordStatusApplied(source, creature, active, active.GetRemainingDurationSeconds(Clock.UtcNow), false, true);

            _statusEffect.NotifyStatusApplied(source, creature, active.Categories);
            if ((active.Categories & StatusEffectCategory.Debuff) != 0 &&
                source != creature && GetIsReactionTypeHostile(creature, source))
            {
                _statusEffect.NotifyHostileDebuffApplied(source, GetActiveAbilityImpactSummary(source)?.Ability);
            }
            _statusEffect.PublishStatusEffectReceivedRefresh(creature);

            var durationResistanceMessage = StatusEffectRules.BuildDurationResistanceMessage(
                resistanceType, active.Name, durationTicksWithoutResistance, durationTicks, active.Frequency);
            if (!string.IsNullOrWhiteSpace(durationResistanceMessage) &&
                (GetIsPC(source) || GetIsDM(source)))
            {
                SendMessageToPC(source, durationResistanceMessage);
            }
        }

        private float ApplyOutgoingStatusDurationAdjustments(
            IStatusEffect statusEffect,
            uint source,
            float durationSeconds,
            bool isPermanent)
        {
            if (isPermanent || durationSeconds <= 0 || !GetIsObjectValid(source))
                return durationSeconds;

            var percentAdjustment = StatusEffectRules.GetOutgoingDurationPercentAdjustment(statusEffect.Categories, stat => GetCreatureStatAdjustment(source, stat));

            durationSeconds = StatusEffectRules.CalculateAdjustedStatusDurationSeconds(durationSeconds, percentAdjustment);

            if ((statusEffect.Categories & StatusEffectCategory.Bleeding) == StatusEffectCategory.Bleeding)
            {
                var bonusSeconds = GetCreatureStatAdjustment(source, StatType.OutgoingBleedingDurationBonusSeconds);
                if (bonusSeconds > 0)
                {
                    durationSeconds += bonusSeconds;
                }
            }

            return durationSeconds;
        }

        private void ApplyOutgoingStatusStatAdjustments(IStatusEffect statusEffect, uint source)
        {
            if (!GetIsObjectValid(source))
                return;

            if ((statusEffect.Categories & StatusEffectCategory.ForceDisruption) == StatusEffectCategory.ForceDisruption)
            {
                var forceDefenseAdjustment = GetCreatureStatAdjustment(source, StatType.OutgoingForceDisruptionForceDefensePercentAdjustment);
                if (forceDefenseAdjustment != 0)
                {
                    statusEffect.StatGroup.Stats[StatType.ForceDefensePercentAdjustment] += forceDefenseAdjustment;
                }
            }
        }

        private ResistanceType ResolveResistanceType(
            IStatusEffect statusEffect,
            ResistanceType resistanceOverride,
            CombatDamageType sourceDamageType)
        {
            if (_resistance.IsValidResistanceType(resistanceOverride))
                return resistanceOverride;

            if (_resistance.IsValidResistanceType(statusEffect.ResistanceType))
                return statusEffect.ResistanceType;

            if (sourceDamageType.TryGetElementalResistanceType(out var elementalSourceType))
                return elementalSourceType;

            if (sourceDamageType.TryGetSourceResistanceType(out var sourceResistanceType))
                return sourceResistanceType;

            return ResistanceType.Invalid;
        }

        public bool ExtendStatusEffectDuration(
            uint creature,
            Type statusEffectClass,
            uint source,
            float durationSeconds)
        {
            if (!_statusEffect.TryGetCreatureStatusEffects(creature, out var creatureEffects) ||
                statusEffectClass == null ||
                durationSeconds <= 0f)
            {
                return false;
            }

            var extended = false;
            foreach (var statusEffect in creatureEffects.GetAllEffects())
            {
                if ((statusEffect.Categories & StatusEffectCategory.HardCrowdControl) != 0 ||
                    statusEffect.GetType() != statusEffectClass)
                    continue;

                if (source != OBJECT_INVALID && statusEffect.Source != source)
                    continue;

                statusEffect.SetDurationSeconds(statusEffect.GetRemainingDurationSeconds(Clock.UtcNow) + durationSeconds);
                _statusEffect.ReplaceNativeStatusEffect(creature, statusEffect, statusEffect.DurationTicks, statusEffect.DurationTicks < 0);
                extended = true;
                if (_combatRecorder.IsRecording)
                    _combatRecorder.RecordStatusApplied(source, creature, statusEffect, statusEffect.GetRemainingDurationSeconds(Clock.UtcNow), false, true);
            }

            return extended;
        }

        public bool RefreshStatusEffectDuration(
            uint creature,
            Type statusEffectClass,
            uint source,
            float durationSeconds,
            ResistanceType resistanceOverride = ResistanceType.Invalid,
            CombatDamageType sourceDamageType = CombatDamageType.Invalid)
        {
            if (!_statusEffect.TryGetCreatureStatusEffects(creature, out var creatureEffects) ||
                statusEffectClass == null ||
                durationSeconds <= 0f)
            {
                return false;
            }

            var refreshed = false;
            foreach (var statusEffect in creatureEffects.GetAllEffects())
            {
                if ((statusEffect.Categories & StatusEffectCategory.HardCrowdControl) != 0 ||
                    statusEffect.GetType() != statusEffectClass)
                    continue;

                if (source != OBJECT_INVALID && statusEffect.Source != source)
                    continue;

                var adjustedSeconds = ApplyOutgoingStatusDurationAdjustments(statusEffect, source, durationSeconds, false);
                var ticks = Math.Max(1, (int)Math.Ceiling(adjustedSeconds / Math.Max(1f, statusEffect.Frequency)));
                var originalTicks = ticks;
                var resistanceType = ResolveResistanceType(statusEffect, resistanceOverride, sourceDamageType);
                if (_resistance.IsValidResistanceType(resistanceType) &&
                    GetIsObjectValid(source) &&
                    GetIsObjectValid(creature) &&
                    GetIsReactionTypeHostile(creature, source))
                {
                    if (_resistance.HasImmunity(creature, resistanceType))
                    {
                        SendMessageToPC(source, "Your ability was resisted.");
                        continue;
                    }

                    ticks = _resistance.CalculateResistedTicks(creature, resistanceType, ticks);
                }

                if (ticks <= 0)
                {
                    SendMessageToPC(source, "Your ability was resisted.");
                    continue;
                }

                adjustedSeconds = Math.Min(adjustedSeconds + Math.Max(0, ticks - originalTicks) * Math.Max(1f, statusEffect.Frequency),
                    ticks * Math.Max(1f, statusEffect.Frequency));
                statusEffect.SetDurationSeconds(Math.Max(statusEffect.GetRemainingDurationSeconds(Clock.UtcNow), adjustedSeconds));
                _statusEffect.ReplaceNativeStatusEffect(creature, statusEffect, statusEffect.DurationTicks, statusEffect.DurationTicks < 0);
                refreshed = true;
                if (_combatRecorder.IsRecording)
                    _combatRecorder.RecordStatusApplied(source, creature, statusEffect, statusEffect.GetRemainingDurationSeconds(Clock.UtcNow), false, true);
            }

            return refreshed;
        }

        protected virtual int GetCreatureStatAdjustment(uint creature, StatType stat) => _stat.Value.GetStatAdjustment(creature, stat);

        protected virtual AbilityImpactSummary GetActiveAbilityImpactSummary(uint activator) => _activeImpact.GetActiveAbilityImpactSummary(activator);
    }
}
