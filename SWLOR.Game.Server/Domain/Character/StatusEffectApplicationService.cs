using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// Applying status effects (part 3 of the status effect split): the public <c>ApplyStatusEffect</c> family, the checks, the
    /// replaced and conflicting effects, the activation and the messages. The trackers, the removal, the durations and the native
    /// effect stay in <see cref="StatusEffectService"/>; this part calls them through <see cref="IStatusEffectService"/>, the timing and refresh
    /// through <see cref="IStatusEffectDurationService"/>, the swing deferral through <see cref="IStatusEffectCombatEventService"/> and the native effect through <see cref="IStatusEffectNativeService"/>. The
    /// application is cut into named steps that keep the original's order of reads and writes. The active ability impact sits behind
    /// a one-line <c>protected virtual</c> method whose body is the original call.
    /// </summary>
    public class StatusEffectApplicationService : IStatusEffectApplicationService
    {
        private readonly IStatusEffectService _statusEffect;
        private readonly IStatusEffectCatalogService _statusCatalog;
        private readonly IMessagingService _messaging;
        private readonly IPlayerNameService _playerName;
        private readonly IActiveAbilityImpact _activeImpact;
        private readonly IStatusEffectDurationService _duration;
        private readonly IStatusEffectCombatEventService _combatEvents;
        private readonly IStatusEffectNativeService _native;

        public StatusEffectApplicationService(
            IStatusEffectService statusEffect,
            IStatusEffectCatalogService statusCatalog,
            IMessagingService messaging,
            IPlayerNameService playerName,
            IActiveAbilityImpact activeImpact,
            IStatusEffectDurationService duration,
            IStatusEffectCombatEventService combatEvents,
            IStatusEffectNativeService native)
        {
            _statusEffect = statusEffect;
            _statusCatalog = statusCatalog;
            _messaging = messaging;
            _playerName = playerName;
            _activeImpact = activeImpact;
            _duration = duration;
            _combatEvents = combatEvents;
            _native = native;
        }

        public void ApplyPermanentStatusEffect<T>(uint source, uint creature)
            where T: IStatusEffect
        {
            ApplyAndProfileStatusEffect((IStatusEffect)Activator.CreateInstance(typeof(T)), source, creature, -1, true);
        }

        public void ApplyPermanentStatusEffect(Type type, uint source, uint creature)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            ApplyAndProfileStatusEffect((IStatusEffect)Activator.CreateInstance(type), source, creature, -1, true);
        }

        private bool ApplyAndProfileStatusEffect(
            IStatusEffect statusEffect,
            uint source,
            uint creature,
            float durationSeconds,
            bool isPermanent,
            ResistanceType resistanceOverride = ResistanceType.Invalid,
            CombatDamageType sourceDamageType = CombatDamageType.Invalid,
            Type replacedStatusEffectType = null)
        {
            // A deferred application is timed when it runs after the swing.
            if (_combatEvents.IsDeferredNativeAttackStatusEffect(statusEffect, creature))
                return RecordCapstoneProof(ApplyStatusEffectInternal(statusEffect, source, creature, durationSeconds, isPermanent,
                    resistanceOverride, sourceDamageType, replacedStatusEffectType), statusEffect, source, creature);

            var profile = FrameProfiler.Start();
            try
            {
                return RecordCapstoneProof(ApplyStatusEffectInternal(statusEffect, source, creature, durationSeconds, isPermanent,
                    resistanceOverride, sourceDamageType, replacedStatusEffectType), statusEffect, source, creature);
            }
            finally
            {
                FrameProfiler.Stop(FrameMetric.StatusEffectApply, profile, statusEffect.Name);
            }
        }

        // Capstone quest steps count a kill only after the player used their skill's mechanic on it.
        private bool RecordCapstoneProof(bool applied, IStatusEffect statusEffect, uint source, uint creature)
        {
            if (applied)
                _statusEffect.NotifyApplicationCompleted(source, creature, statusEffect.GetType(),
                    statusEffect.OriginatingAbility ?? GetActiveAbilityImpactSummary(source)?.Ability);

            return applied;
        }

        public bool ApplyStatusEffect<T>(
            uint source,
            uint creature,
            float durationSeconds,
            ResistanceType resistanceOverride = ResistanceType.Invalid)
            where T: IStatusEffect
        {
            return ApplyStatusEffect(source, creature, (IStatusEffect)Activator.CreateInstance(typeof(T)), durationSeconds, resistanceOverride);
        }

        public bool ApplyStatusEffect<T>(
            uint source,
            uint creature,
            float durationSeconds,
            CombatDamageType sourceDamageType)
            where T : IStatusEffect
        {
            return ApplyStatusEffect(source, creature, (IStatusEffect)Activator.CreateInstance(typeof(T)), durationSeconds, sourceDamageType);
        }

        public bool ApplyStatusEffect(
            uint source,
            uint creature,
            IStatusEffect statusEffect,
            float durationSeconds,
            ResistanceType resistanceOverride = ResistanceType.Invalid)
        {
            return ApplyAndProfileStatusEffect(
                statusEffect,
                source,
                creature,
                durationSeconds,
                durationSeconds <= 0f,
                resistanceOverride);
        }

        public bool ApplyStatusEffect(
            uint source,
            uint creature,
            IStatusEffect statusEffect,
            float durationSeconds,
            CombatDamageType sourceDamageType)
        {
            return ApplyAndProfileStatusEffect(
                statusEffect,
                source,
                creature,
                durationSeconds,
                durationSeconds <= 0f,
                ResistanceType.Invalid,
                sourceDamageType);
        }

        public bool ApplyStatusEffect(
            uint source,
            uint creature,
            Type statusEffectClass,
            float durationSeconds,
            ResistanceType resistanceOverride = ResistanceType.Invalid)
        {
            if (!_statusCatalog.TryCreateStatusEffect(statusEffectClass, out var statusEffect))
                throw new KeyNotFoundException($"Status effect '{statusEffectClass?.Name ?? "null"}' is not registered.");

            return ApplyAndProfileStatusEffect(
                statusEffect,
                source,
                creature,
                durationSeconds,
                durationSeconds <= 0f,
                resistanceOverride);
        }

        public bool ApplyStatusEffect(
            uint source,
            uint creature,
            Type statusEffectClass,
            float durationSeconds,
            CombatDamageType sourceDamageType,
            Type replacedStatusEffectType = null)
        {
            if (!_statusCatalog.TryCreateStatusEffect(statusEffectClass, out var statusEffect))
                throw new KeyNotFoundException($"Status effect '{statusEffectClass?.Name ?? "null"}' is not registered.");

            return ApplyAndProfileStatusEffect(
                statusEffect,
                source,
                creature,
                durationSeconds,
                durationSeconds <= 0f,
                ResistanceType.Invalid,
                sourceDamageType,
                replacedStatusEffectType);
        }

        private bool ApplyStatusEffectInternal(
            IStatusEffect statusEffect,
            uint source,
            uint creature,
            float durationSeconds,
            bool isPermanent,
            ResistanceType resistanceOverride = ResistanceType.Invalid,
            CombatDamageType sourceDamageType = CombatDamageType.Invalid,
            Type replacedStatusEffectType = null)
        {
            if (TryDeferNativeAttackStatusEffect(
                    statusEffect,
                    source,
                    creature,
                    durationSeconds,
                    isPermanent,
                    resistanceOverride,
                    sourceDamageType,
                    replacedStatusEffectType))
            {
                return true;
            }

            if (!_duration.ResolveApplicationTiming(
                    statusEffect, source, creature, durationSeconds, isPermanent, resistanceOverride, sourceDamageType, out var timing))
            {
                return false;
            }

            if (!ResolveReplacedEffects(
                    statusEffect, creature, isPermanent, replacedStatusEffectType, ref timing, out var existingEffects, out var replacedEffects))
            {
                return false;
            }

            if (!ValidateApplication(statusEffect, source, creature, existingEffects, replacedEffects))
                return false;

            if (!isPermanent && replacedStatusEffectType == null &&
                _duration.TryRefreshInPlace(statusEffect, source, creature, timing.DurationTicks, timing.DurationTicksWithoutResistance,
                    Math.Min(timing.DurationSeconds, timing.DurationTicks * timing.Frequency), timing.ResistanceType))
            {
                return true;
            }

            var tickAnchor = ClearConflictingEffects(statusEffect, source, creature, replacedStatusEffectType);
            if (!ActivateEffect(statusEffect, source, creature, isPermanent, timing, tickAnchor))
                return false;

            AnnounceApplication(statusEffect, source, creature, timing);
            return true;
        }

        // Step 1: a limited attack-timing effect granted while a native attack cycle resolves waits for the cycle to end, then
        // applies as if it had just been granted. The closure is built only when the application is deferred.
        private bool TryDeferNativeAttackStatusEffect(
            IStatusEffect statusEffect,
            uint source,
            uint creature,
            float durationSeconds,
            bool isPermanent,
            ResistanceType resistanceOverride,
            CombatDamageType sourceDamageType,
            Type replacedStatusEffectType)
        {
            if (!_combatEvents.IsDeferredNativeAttackStatusEffect(statusEffect, creature))
                return false;

            _combatEvents.DeferNativeAttackStatusEffect(creature, () => ApplyAndProfileStatusEffect(
                statusEffect,
                source,
                creature,
                durationSeconds,
                isPermanent,
                resistanceOverride,
                sourceDamageType,
                replacedStatusEffectType));
            return true;
        }

        // Step 3: the effects a conversion replaces, and the control time they leave. False when the effect to replace is gone or no
        // control time is left.
        private bool ResolveReplacedEffects(
            IStatusEffect statusEffect,
            uint creature,
            bool isPermanent,
            Type replacedStatusEffectType,
            ref StatusApplicationTiming timing,
            out CreatureStatusEffect existingEffects,
            out IStatusEffect[] replacedEffects)
        {
            existingEffects = replacedStatusEffectType == null ? null : _statusEffect.GetCreatureStatusEffects(creature);
            replacedEffects = existingEffects?.GetAllEffects()
                .Where(effect => effect.GetType() == replacedStatusEffectType).ToArray() ?? Array.Empty<IStatusEffect>();
            if (replacedStatusEffectType != null && replacedEffects.Length == 0)
                return false;

            if (!isPermanent && (statusEffect.Categories & StatusEffectCategory.HardCrowdControl) != 0)
            {
                var durationTicks = timing.DurationTicks;
                var durationTicksWithoutResistance = timing.DurationTicksWithoutResistance;
                var now = Clock.UtcNow;
                foreach (var replaced in replacedEffects.Where(effect =>
                             (effect.Categories & StatusEffectCategory.HardCrowdControl) != 0))
                {
                    var remainingSeconds = replaced.GetRemainingDurationSeconds(now);
                    durationTicks = StatusEffectRules.ClampConvertedControlDurationTicks(durationTicks, statusEffect.Frequency, remainingSeconds);
                    durationTicksWithoutResistance = StatusEffectRules.ClampConvertedControlDurationTicks(
                        durationTicksWithoutResistance, statusEffect.Frequency, remainingSeconds);
                }

                timing = timing with { DurationTicks = durationTicks, DurationTicksWithoutResistance = durationTicksWithoutResistance };
                if (durationTicks <= 0)
                    return false;
            }

            return true;
        }

        // Step 4: the effect's own check, without counting the effects it replaces, and the more powerful effects that block it.
        private bool ValidateApplication(
            IStatusEffect statusEffect,
            uint source,
            uint creature,
            CreatureStatusEffect existingEffects,
            IStatusEffect[] replacedEffects)
        {
            // A conversion validates without counting the effect it replaces as active
            // control. Preserve its native effect, expiration and stats on rejection.
            string canApply;
            foreach (var effect in replacedEffects)
                existingEffects.Remove(effect);
            try { canApply = statusEffect.CanApply(creature); }
            finally
            {
                foreach (var effect in replacedEffects)
                    existingEffects.Add(effect);
            }
            if (!string.IsNullOrWhiteSpace(canApply))
            {
                var message = $"Effect failed to apply: {canApply}";
                SendStatusEffectFailure(source, creature, message);
                return false;
            }

            foreach (var morePowerful in statusEffect.MorePowerfulEffectTypes)
            {
                if (_statusEffect.HasEffect(morePowerful, creature))
                {
                    var message = "A more powerful effect is active.";
                    SendStatusEffectFailure(source, creature, message);
                    return false;
                }
            }

            return true;
        }

        // Step 6: removes what the new effect replaces or overrides, and answers the tick anchor a refresh of the same effect keeps.
        private DateTime? ClearConflictingEffects(
            IStatusEffect statusEffect,
            uint source,
            uint creature,
            Type replacedStatusEffectType)
        {
            if (replacedStatusEffectType != null)
                _statusEffect.RemoveStatusEffectForReplacement(replacedStatusEffectType, creature, OBJECT_INVALID);

            var creatureEffects = _statusEffect.EnsureCreatureStatusEffectTracker(creature);

            var tickAnchor = StatusEffectRules.GetRefreshTickAnchor(statusEffect, creatureEffects.GetAllEffects(), source);

            _statusEffect.RemoveOtherCommandStatuses(creature, statusEffect.GetType(), source);

            switch (statusEffect.StackingType)
            {
                case StatusEffectStackType.Disabled:
                case StatusEffectStackType.Invalid:
                    _statusEffect.RemoveStatusEffectForReplacement(statusEffect.GetType(), creature, OBJECT_INVALID);
                    break;
                case StatusEffectStackType.StackFromMultipleSources:
                    _statusEffect.RemoveStatusEffectForReplacement(statusEffect.GetType(), creature, source);
                    break;
            }

            foreach (var lessPowerful in statusEffect.LessPowerfulEffectTypes)
            {
                _statusEffect.RemoveStatusEffectForReplacement(lessPowerful, creature, OBJECT_INVALID);
            }

            return tickAnchor;
        }

        // Step 7: applies the effect, tracks it and tells the listeners. False when the effect flagged itself for removal.
        private bool ActivateEffect(
            IStatusEffect statusEffect,
            uint source,
            uint creature,
            bool isPermanent,
            StatusApplicationTiming timing,
            DateTime? tickAnchor)
        {
            statusEffect.AssignResistanceType(timing.ResistanceType);
            statusEffect.OriginatingAbility ??= GetActiveAbilityImpactSummary(source)?.Ability;
            statusEffect.ApplyEffect(source, creature, timing.DurationTicks,
                isPermanent ? 0f : Math.Min(timing.DurationSeconds, timing.DurationTicks * timing.Frequency), tickAnchor);
            if (statusEffect.IsFlaggedForRemoval)
            {
                statusEffect.RemoveEffect(creature);
                if (_statusEffect.TryGetCreatureStatusEffects(creature, out var remaining) &&
                    remaining.GetAllEffects().Count <= 0)
                {
                    _statusEffect.RemoveCreature(creature);
                }

                return false;
            }

            var creatureEffects = _statusEffect.EnsureCreatureStatusEffectTracker(creature);
            creatureEffects.Add(statusEffect);

            if (statusEffect is ILeadershipDamageReductionStatusEffect)
            {
                LeadershipDamageReductionRules.ReconcileLeadershipDamageReduction(creatureEffects);
            }

            if (_statusEffect.HasStatAdjustment(statusEffect))
            {
                _statusEffect.ApplyCreatureMovementRate(creature);
            }

            _native.ApplyTrackedNWNEffect(creature, statusEffect, statusEffect.DurationTicks, isPermanent);
            _statusEffect.NotifyStatusApplied(source, creature, statusEffect.Categories);
            if ((statusEffect.Categories & StatusEffectCategory.Debuff) != 0 &&
                source != creature && GetIsReactionTypeHostile(creature, source))
            {
                _statusEffect.NotifyHostileDebuffApplied(source, GetActiveAbilityImpactSummary(source)?.Ability);
            }
            _statusEffect.PublishStatusEffectReceivedRefresh(creature);
            return true;
        }

        // Step 8: tells the source how much the duration was cut, and tells the people nearby that the creature received the effect.
        private void AnnounceApplication(IStatusEffect statusEffect, uint source, uint creature, StatusApplicationTiming timing)
        {
            // Compare effective durations so the control budget is not mislabeled as resistance,
            // and vulnerability never claims to extend control beyond its actual expiration.
            var durationResistanceMessage = StatusEffectRules.BuildDurationResistanceMessage(
                timing.ResistanceType, statusEffect.Name, timing.DurationTicksWithoutResistance, timing.DurationTicks, statusEffect.Frequency);
            if (!string.IsNullOrWhiteSpace(durationResistanceMessage) &&
                (GetIsPC(source) || GetIsDM(source)))
            {
                SendMessageToPC(source, durationResistanceMessage);
            }

            if (statusEffect.SendsApplicationMessage)
            {
                string BuildApplicationMessage(uint receiver)
                {
                    var name = _playerName.GetDisplayName(receiver, creature);
                    var effectName = statusEffect.Name;
                    var applicationMessage = $"{name} receives the effect of {effectName}";
                    if (GetIsObjectValid(source) && source != creature)
                    {
                        var sourceName = _playerName.GetDisplayName(receiver, source);
                        if (!string.IsNullOrWhiteSpace(sourceName))
                        {
                            applicationMessage = $"{applicationMessage} from {sourceName}";
                        }
                    }

                    return applicationMessage;
                }

                var sourceIsOther = GetIsObjectValid(source) && source != creature;
                if (StatusEffectRules.ShouldAnnounceNearby(ConcealmentRules.IsConcealed(creature), sourceIsOther && ConcealmentRules.IsConcealed(source)))
                {
                    _messaging.SendMessageNearbyToPlayers(creature, BuildApplicationMessage);
                }
                else
                {
                    // A nearby announcement would name a hidden creature to everyone around it.
                    SendMessageToPC(creature, BuildApplicationMessage(creature));
                    if (sourceIsOther && GetIsPC(source))
                        SendMessageToPC(source, BuildApplicationMessage(source));
                }
            }
        }

        private void SendStatusEffectFailure(uint source, uint creature, string message)
        {
            var recipient = GetIsObjectValid(source) && (GetIsPC(source) || GetIsDM(source))
                ? source
                : creature;

            SendMessageToPC(recipient, message);
        }

        protected virtual AbilityImpactSummary GetActiveAbilityImpactSummary(uint activator) => _activeImpact.GetActiveAbilityImpactSummary(activator);
    }
}
