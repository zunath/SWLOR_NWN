using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// Status effects: the per-creature trackers, the native
    /// effect that carries each status and the effects granted while a native attack cycle resolves. The logic that used
    /// to be static in the facade <c>StatusEffect</c> is moved here unchanged; the event handlers run on the container's
    /// instance, and static callers reach it through <c>GameServices.StatusEffect</c>. Calls into <c>Stat</c>, <c>Ability</c> and
    /// <c>Combat</c>, the feature code, the native effect read and the definition search sit behind one-line
    /// <c>protected virtual</c> methods whose bodies are the original calls. The pure duration rules live in
    /// <see cref="StatusEffectRules"/>, for code that has no container.
    /// </summary>
    public class StatusEffectService : IStatusEffectService
    {
        private const string StatusEffectTag = "STATUS_EFFECT";
        private const float Interval = 1f;

        [ReloadState(ReloadStateKind.Carried)]
        private readonly Dictionary<uint, CreatureStatusEffect> _creatureEffects = new();
        [ReloadState(ReloadStateKind.Runtime)]
        private readonly List<ILoggedOutStatusEffectSource> _loggedOutSources = new();
        [ReloadState(ReloadStateKind.Runtime)]
        private readonly HashSet<string> _suppressedStatusEffectRemovalIds = new();
        [ReloadState(ReloadStateKind.Runtime)]
        private readonly List<IStatEffectListener> _statEffectListeners = new();
        [ReloadState(ReloadStateKind.Runtime)]
        private readonly List<IStatusEffectMeleeListener> _meleeListeners = new();
        [ReloadState(ReloadStateKind.Runtime)]
        private readonly List<IStatusEffectAppliedListener> _appliedListeners = new();
        [ReloadState(ReloadStateKind.Runtime)]
        private readonly Dictionary<uint, int> _nativeAttackSwingDepth = new();
        [ReloadState(ReloadStateKind.Runtime)]
        private readonly Dictionary<uint, List<Action>> _deferredNativeAttackStatusEffects = new();
        private readonly IStatusResistance _resistance;
        private readonly IStatusEffectCatalogService _statusCatalog;
        private readonly ICharacterRecording _combatRecorder;
        private readonly IMessagingService _messaging;
        private readonly IPlayerNameService _playerName;
        private readonly IGuiRefreshPublisher _gui;
        private readonly Lazy<IStatService> _stat;
        private readonly IActiveAbilityImpact _activeImpact;

        public StatusEffectService(
            IStatusResistance resistance,
            IStatusEffectCatalogService statusCatalog,
            ICharacterRecording combatRecorder,
            IMessagingService messaging,
            IPlayerNameService playerName,
            IGuiRefreshPublisher gui,
            Lazy<IStatService> stat,
            IActiveAbilityImpact activeImpact)
        {
            _resistance = resistance;
            _statusCatalog = statusCatalog;
            _combatRecorder = combatRecorder;
            _messaging = messaging;
            _playerName = playerName;
            _gui = gui;
            _stat = stat;
            _activeImpact = activeImpact;
        }

        /// <summary>
        /// Registers who is told when a player's status effects are restored and when a stat-adjusting effect changes. The list holds
        /// service instances, so a reload rebuilds it through the boot handler that registers the listener (it reruns before the
        /// carried status effects are adopted).
        /// </summary>
        public void RegisterStatEffectListener(IStatEffectListener listener)
        {
            if (!_statEffectListeners.Contains(listener))
                _statEffectListeners.Add(listener);
        }

        /// <summary>
        /// Registers who is told about stance removal and direct hits about to land. Registering the same listener twice keeps one
        /// entry; the list holds service instances, so a reload rebuilds it through the boot handler that registers the listener.
        /// </summary>
        public void RegisterMeleeListener(IStatusEffectMeleeListener listener)
        {
            if (!_meleeListeners.Contains(listener))
                _meleeListeners.Add(listener);
        }

        /// <summary>
        /// Registers who is told when a status effect is applied or refreshed, in registration order. Registering the same
        /// listener twice keeps one entry; a reload rebuilds the list through the boot handlers that register the listeners.
        /// </summary>
        public void RegisterAppliedListener(IStatusEffectAppliedListener listener)
        {
            if (!_appliedListeners.Contains(listener))
                _appliedListeners.Add(listener);
        }

        public void ClearStatusEffectsOnDeath(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            RemoveAllStatusEffects(player);
            if (!_creatureEffects.TryGetValue(player, out var remaining) || remaining.GetAllEffects().Count == 0)
                RemoveCreature(player);
        }

        /// <summary>
        /// Handle when a status effect is applied (called by NWN script)
        /// </summary>
        [NWNEventHandler(ScriptName.OnApplyStatusEffect)]
        public void OnApplyStatusEffect()
        {
            OnApplyNWNStatusEffect(OBJECT_SELF);
        }

        /// <summary>
        /// Handle when a status effect is removed (called by NWN script)
        /// </summary>
        [NWNEventHandler(ScriptName.OnRemoveStatusEffect)]
        public void OnRemoveStatusEffect()
        {
            OnRemoveNWNStatusEffect(OBJECT_SELF);
        }

        /// <summary>
        /// Handle status effect interval processing (called by NWN script)
        /// </summary>
        [NWNEventHandler(ScriptName.OnStatusEffectInterval)]
        public void OnStatusEffectInterval()
        {
            OnNWNStatusEffectInterval(OBJECT_SELF);
        }

        /// <summary>
        /// Registers who holds the effects of players who logged out, so a source-wide removal reaches them too. The list holds service
        /// instances, so a reload rebuilds it through the boot handler that registers the holder (it reruns before the carried status
        /// effects are adopted). Registering the same holder twice keeps one entry.
        /// </summary>
        public void RegisterLoggedOutSource(ILoggedOutStatusEffectSource source)
        {
            if (!_loggedOutSources.Contains(source))
                _loggedOutSources.Add(source);
        }

        private IEnumerable<CreatureStatusEffect> GetLoggedOutEffects()
        {
            return _loggedOutSources.SelectMany(source => source.GetLoggedOutEffects());
        }

        public bool TryGetCreatureStatusEffects(uint creature, out CreatureStatusEffect effects)
        {
            return _creatureEffects.TryGetValue(creature, out effects);
        }

        public IReadOnlyCollection<uint> GetTrackedCreatures()
        {
            return _creatureEffects.Keys;
        }

        public void TrackCreatureStatusEffects(uint creature, CreatureStatusEffect effects)
        {
            _creatureEffects[creature] = effects;
        }

        private CreatureStatusEffect EnsureCreatureStatusEffectTracker(uint creature)
        {
            if (!_creatureEffects.TryGetValue(creature, out var effects))
            {
                effects = new CreatureStatusEffect();
                _creatureEffects[creature] = effects;
            }

            return effects;
        }

        public void OnApplyNWNStatusEffect(uint player)
        {
            if (!_creatureEffects.ContainsKey(player))
                _creatureEffects[player] = new CreatureStatusEffect();
        }

        public void OnRemoveNWNStatusEffect(uint player)
        {
            var tag = GetLastStatusEffectTag();
            if (string.IsNullOrWhiteSpace(tag) || tag == StatusEffectTag)
            {
                if (_creatureEffects.ContainsKey(player))
                    _creatureEffects.Remove(player);

                return;
            }

            if (_suppressedStatusEffectRemovalIds.Contains(tag))
                return;

            // NWN can deliver the old timer's removal callback after a duration extension
            // has already replaced it with another native effect carrying the same tag.
            // Wait until native removal settles, then only clear statuses without a timer.
            DelayCommand(0f, () =>
            {
                if (!MayHaveEffectTag(player, tag) || !HasEffectByTag(player, tag))
                    RemoveStatusEffectById(player, tag, true, false);
            });
        }

        private string GetLastStatusEffectTag()
        {
            var effect = GetLastRunScriptEffect();
            var tag = GetEffectTag(effect);
            return !string.IsNullOrWhiteSpace(tag)
                ? tag
                : GetEffectString(effect, 0);
        }

        public void OnNWNStatusEffectInterval(uint creature)
        {
            var tag = GetLastStatusEffectTag();
            if (string.IsNullOrWhiteSpace(tag) || tag == StatusEffectTag)
            {
                TickAllStatusEffects(creature);
                return;
            }

            TickStatusEffectById(creature, tag);
        }

        private void TickAllStatusEffects(uint creature)
        {
            // Clean up invalid creatures when we encounter them
            if (!GetIsObjectValid(creature) || GetIsDead(creature))
            {
                if (_creatureEffects.ContainsKey(creature))
                {
                    _creatureEffects.Remove(creature);
                }
                RemoveEffectByTag(creature, StatusEffectTag);
                return;
            }

            if (!_creatureEffects.ContainsKey(creature))
            {
                RemoveEffectByTag(creature, StatusEffectTag);
                return;
            }

            var effects = _creatureEffects[creature];

            foreach (var effect in effects.GetAllTickEffects())
            {
                if (effect.ActivationType != StatusEffectActivationType.Tick)
                    continue;

                TickStatusEffect(creature, effect);
            }
        }

        private void TickStatusEffectById(uint creature, string statusEffectId)
        {
            if (!GetIsObjectValid(creature) || GetIsDead(creature))
            {
                RemoveStatusEffectById(creature, statusEffectId, false, true);
                return;
            }

            if (!_creatureEffects.TryGetValue(creature, out var effects))
            {
                RemoveEffectByTag(creature, statusEffectId);
                return;
            }

            var effect = effects.GetAllTickEffects().FirstOrDefault(x => x.Id == statusEffectId);
            if (effect == null)
            {
                RemoveEffectByTag(creature, statusEffectId);
                return;
            }

            TickStatusEffect(creature, effect);
        }

        private void TickStatusEffect(uint creature, IStatusEffect effect)
        {
            if (effect.ActivationType != StatusEffectActivationType.Tick)
                return;

            if (effect.IsFlaggedForRemoval)
            {
                RemoveStatusEffectById(creature, effect.Id, true, true);
            }
            else
            {
                effect.TickEffect(creature);

                if (effect.IsFlaggedForRemoval)
                {
                    RemoveStatusEffectById(creature, effect.Id, true, true);
                }
            }
        }

        public CreatureStatusEffect GetCreatureStatusEffects(uint creature)
        {
            return !_creatureEffects.ContainsKey(creature)
                ? new CreatureStatusEffect()
                : _creatureEffects[creature];
        }

        public IReadOnlyList<StatAdjustmentSource> GetStatSources(uint creature, StatType payloadStat)
        {
            return _creatureEffects.TryGetValue(creature, out var effects)
                ? effects.GetStatSources(payloadStat)
                : Array.Empty<StatAdjustmentSource>();
        }

        public int GetStatAdjustment(uint creature, StatType stat)
        {
            return _creatureEffects.TryGetValue(creature, out var effects) &&
                   effects.StatGroup.Stats.TryGetValue(stat, out var value)
                ? value
                : 0;
        }

        public bool HasAnyActiveEffect(uint creature, IReadOnlySet<Type> effectTypes)
        {
            return _creatureEffects.TryGetValue(creature, out var effects) && effects.HasAnyActiveEffect(effectTypes);
        }

        public bool HasAnyActiveEffect(uint creature, StatusEffectSourceType sourceType)
        {
            return _creatureEffects.TryGetValue(creature, out var effects) && effects.HasAnyActiveEffect(sourceType);
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
            if (IsDeferredNativeAttackStatusEffect(statusEffect, creature))
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
                NotifyApplicationCompleted(source, creature, statusEffect.GetType(),
                    statusEffect.OriginatingAbility ?? GetActiveAbilityImpactSummary(source)?.Ability);

            return applied;
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

            var existingEffects = replacedStatusEffectType == null ? null : GetCreatureStatusEffects(creature);
            var replacedEffects = existingEffects?.GetAllEffects()
                .Where(effect => effect.GetType() == replacedStatusEffectType).ToArray() ?? Array.Empty<IStatusEffect>();
            if (replacedStatusEffectType != null && replacedEffects.Length == 0)
                return false;

            if (!isPermanent && (statusEffect.Categories & StatusEffectCategory.HardCrowdControl) != 0)
            {
                var now = Clock.UtcNow;
                foreach (var replaced in replacedEffects.Where(effect =>
                             (effect.Categories & StatusEffectCategory.HardCrowdControl) != 0))
                {
                    var remainingSeconds = replaced.GetRemainingDurationSeconds(now);
                    durationTicks = StatusEffectRules.ClampConvertedControlDurationTicks(durationTicks, statusEffect.Frequency, remainingSeconds);
                    durationTicksWithoutResistance = StatusEffectRules.ClampConvertedControlDurationTicks(
                        durationTicksWithoutResistance, statusEffect.Frequency, remainingSeconds);
                }
                if (durationTicks <= 0)
                    return false;
            }

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
                if (HasEffect(morePowerful, creature))
                {
                    var message = "A more powerful effect is active.";
                    SendStatusEffectFailure(source, creature, message);
                    return false;
                }
            }

            if (!isPermanent && replacedStatusEffectType == null &&
                TryRefreshInPlace(statusEffect, source, creature, durationTicks, durationTicksWithoutResistance,
                    Math.Min(durationSeconds, durationTicks * frequency), resistanceType))
            {
                return true;
            }

            if (replacedStatusEffectType != null)
                RemoveStatusEffect(replacedStatusEffectType, creature, OBJECT_INVALID, false, true, true);

            var creatureEffects = EnsureCreatureStatusEffectTracker(creature);

            var tickAnchor = StatusEffectRules.GetRefreshTickAnchor(statusEffect, creatureEffects.GetAllEffects(), source);

            RemoveOtherCommandStatuses(creature, statusEffect.GetType(), source);

            switch (statusEffect.StackingType)
            {
                case StatusEffectStackType.Disabled:
                case StatusEffectStackType.Invalid:
                    RemoveStatusEffect(statusEffect.GetType(), creature, OBJECT_INVALID, false, true, true);
                    break;
                case StatusEffectStackType.StackFromMultipleSources:
                    RemoveStatusEffect(statusEffect.GetType(), creature, source, false, true, true);
                    break;
            }

            foreach (var lessPowerful in statusEffect.LessPowerfulEffectTypes)
            {
                RemoveStatusEffect(lessPowerful, creature, OBJECT_INVALID, false, true, true);
            }

            statusEffect.AssignResistanceType(resistanceType);
            statusEffect.OriginatingAbility ??= GetActiveAbilityImpactSummary(source)?.Ability;
            statusEffect.ApplyEffect(source, creature, durationTicks,
                isPermanent ? 0f : Math.Min(durationSeconds, durationTicks * frequency), tickAnchor);
            if (statusEffect.IsFlaggedForRemoval)
            {
                statusEffect.RemoveEffect(creature);
                if (_creatureEffects.TryGetValue(creature, out creatureEffects) &&
                    creatureEffects.GetAllEffects().Count <= 0)
                {
                    RemoveCreature(creature);
                }

                return false;
            }

            creatureEffects = EnsureCreatureStatusEffectTracker(creature);
            creatureEffects.Add(statusEffect);

            if (statusEffect is ILeadershipDamageReductionStatusEffect)
            {
                LeadershipDamageReductionRules.ReconcileLeadershipDamageReduction(creatureEffects);
            }

            if (HasStatAdjustment(statusEffect))
            {
                ApplyCreatureMovementRate(creature);
            }

            ApplyTrackedNWNEffect(creature, statusEffect, statusEffect.DurationTicks, isPermanent);
            NotifyStatusApplied(source, creature, statusEffect.Categories);
            if ((statusEffect.Categories & StatusEffectCategory.Debuff) != 0 &&
                source != creature && GetIsReactionTypeHostile(creature, source))
            {
                NotifyHostileDebuffApplied(source, GetActiveAbilityImpactSummary(source)?.Ability);
            }
            PublishStatusEffectReceivedRefresh(creature);

            // Compare effective durations so the control budget is not mislabeled as resistance,
            // and vulnerability never claims to extend control beyond its actual expiration.
            var durationResistanceMessage = StatusEffectRules.BuildDurationResistanceMessage(
                resistanceType, statusEffect.Name, durationTicksWithoutResistance, durationTicks, statusEffect.Frequency);
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

            return true;
        }

        /// <summary>
        /// Restarts an opted-in effect in place when applying it again would produce the same state.
        /// A non-stacking effect from another source is handed to the new source, as a fresh replacement
        /// would be. Duration, tick schedule, native effect and application triggers match a fresh
        /// application; the remove/add cycle and the repeated application message are skipped.
        /// </summary>
        private bool TryRefreshInPlace(
            IStatusEffect statusEffect,
            uint source,
            uint creature,
            int durationTicks,
            int durationTicksWithoutResistance,
            float durationSeconds,
            ResistanceType resistanceType)
        {
            if (statusEffect.StackingType is not (StatusEffectStackType.Disabled or StatusEffectStackType.StackFromMultipleSources) ||
                !_creatureEffects.TryGetValue(creature, out var creatureEffects))
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

            RemoveOtherCommandStatuses(creature, type, source);
            foreach (var lessPowerful in statusEffect.LessPowerfulEffectTypes)
            {
                RemoveStatusEffect(lessPowerful, creature, OBJECT_INVALID, false, true, true);
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
            if (!_creatureEffects.TryGetValue(creature, out var creatureEffects) ||
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
            ReplaceNativeStatusEffect(creature, active, active.DurationTicks, false);
            if (_combatRecorder.IsRecording)
                _combatRecorder.RecordStatusApplied(source, creature, active, active.GetRemainingDurationSeconds(Clock.UtcNow), false, true);

            NotifyStatusApplied(source, creature, active.Categories);
            if ((active.Categories & StatusEffectCategory.Debuff) != 0 &&
                source != creature && GetIsReactionTypeHostile(creature, source))
            {
                NotifyHostileDebuffApplied(source, GetActiveAbilityImpactSummary(source)?.Ability);
            }
            PublishStatusEffectReceivedRefresh(creature);

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

        private void SendStatusEffectFailure(uint source, uint creature, string message)
        {
            var recipient = GetIsObjectValid(source) && (GetIsPC(source) || GetIsDM(source))
                ? source
                : creature;

            SendMessageToPC(recipient, message);
        }

        public void ReapplyNWNEffects(uint creature, CreatureStatusEffect effects)
        {
            var shouldReapplyMovementRate = false;

            foreach (var statusEffect in effects.GetAllEffects())
            {
                statusEffect.ReapplyEffect(creature);
                ApplyTrackedNWNEffect(creature, statusEffect, statusEffect.DurationTicks, statusEffect.DurationTicks < 0);

                if (HasStatAdjustment(statusEffect))
                {
                    shouldReapplyMovementRate = true;
                }
            }

            if (shouldReapplyMovementRate)
            {
                ApplyCreatureMovementRate(creature);
            }
        }

        public void RemoveNonPersistentStatusEffects(uint creature, CreatureStatusEffect effects)
        {
            foreach (var statusEffect in effects.GetAllEffects().Where(effect => !effect.PersistsOnLogout))
            {
                RemoveStatusEffect(creature, statusEffect.GetType(), statusEffect.Source, false);
            }
        }

        public void RemoveTrackedNWNEffects(uint creature, CreatureStatusEffect effects)
        {
            foreach (var statusEffect in effects.GetAllEffects())
            {
                statusEffect.RemoveNativeEffects(creature);
                RemoveNativeStatusEffect(creature, statusEffect.Id);
            }

            RemoveEffectByTag(creature, StatusEffectTag);
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

        private void ApplyTrackedNWNEffect(uint creature, IStatusEffect statusEffect, int durationTicks, bool isPermanent)
        {
            if (MayHaveEffectTag(creature, statusEffect.Id) && HasEffectByTag(creature, statusEffect.Id))
                return;

            ApplyNativeStatusEffect(creature, statusEffect, durationTicks, isPermanent);
        }

        // RemoveEffectByTag hides every effect it finds with the tag, so the presence check that
        // ApplyTrackedNWNEffect makes would always fail here.
        private void ReplaceNativeStatusEffect(uint creature, IStatusEffect statusEffect, int durationTicks, bool isPermanent)
        {
            RemoveNativeStatusEffect(creature, statusEffect.Id);
            ApplyNativeStatusEffect(creature, statusEffect, durationTicks, isPermanent);
        }

        private void ApplyNativeStatusEffect(uint creature, IStatusEffect statusEffect, int durationTicks, bool isPermanent)
        {
            var effect = BuildNativeStatusEffect(statusEffect);

            if (isPermanent || durationTicks < 0)
            {
                ApplyEffectToObject(DurationType.Permanent, effect, creature);
            }
            else
            {
                var durationSeconds = GetStatusEffectDurationSeconds(statusEffect, durationTicks);
                ApplyEffectToObject(DurationType.Temporary, effect, creature, durationSeconds);
            }

            ScheduleTemporaryHitPointLimitCheckForVitality(creature, statusEffect);
        }

        // Native Vitality effects change maximum HP outside the HP budget setters.
        private void ScheduleTemporaryHitPointLimitCheckForVitality(uint creature, IStatusEffect statusEffect)
        {
            if (statusEffect.StatGroup.Abilities.TryGetValue(AbilityType.Vitality, out var vitality) && vitality != 0)
                ScheduleTemporaryHitPointLimitCheck(creature);
        }

        private Effect BuildNativeStatusEffect(IStatusEffect statusEffect)
        {
            var intervalScript = statusEffect.ActivationType == StatusEffectActivationType.Tick
                ? ScriptName.OnStatusEffectInterval
                : string.Empty;

            var effect = EffectRunScript(
                ScriptName.OnApplyStatusEffect,
                ScriptName.OnRemoveStatusEffect,
                intervalScript,
                Interval,
                statusEffect.Id);
            effect = TagEffect(effect, statusEffect.Id);

            if (statusEffect.Icon != EffectIconType.Invalid)
            {
                var iconEffect = TagEffect(EffectIcon(statusEffect.Icon), statusEffect.Id);
                effect = LinkEffect(effect, iconEffect);
            }

            var abilityStatEffects = BuildAbilityStatEffects(statusEffect);
            effect = LinkEffect(effect, abilityStatEffects);
            effect = TagEffect(effect, statusEffect.Id);
            return SupernaturalEffect(effect);
        }

        private float GetStatusEffectDurationSeconds(IStatusEffect statusEffect, int durationTicks)
        {
            var logicalDurationSeconds = statusEffect.DurationTicks > 0
                ? statusEffect.GetRemainingDurationSeconds(Clock.UtcNow)
                : durationTicks * Math.Max(1f, statusEffect.Frequency);

            // NWN may remove an effect before delivering an interval callback scheduled for the
            // exact same timestamp. Ticking effects therefore keep one logical tick of native
            // lifetime grace so a late callback can catch up and the final logical tick can remove
            // the effect.
            // Passive effects have no interval callback and retain their exact duration.
            return statusEffect.ActivationType == StatusEffectActivationType.Tick
                ? logicalDurationSeconds + Math.Max(Interval, statusEffect.Frequency)
                : logicalDurationSeconds;
        }

        private Effect LinkEffect(Effect linkedEffect, Effect effect)
        {
            return effect == null
                ? linkedEffect
                : EffectLinkEffects(linkedEffect, effect);
        }

        private Effect BuildAbilityStatEffects(IStatusEffect statusEffect)
        {
            Effect linkedEffect = null;

            foreach (var (ability, amount) in statusEffect.StatGroup.Abilities)
            {
                if (ability == AbilityType.Invalid || amount == 0)
                    continue;

                var effect = amount > 0
                    ? EffectAbilityIncrease(ability, amount)
                    : EffectAbilityDecrease(ability, Math.Abs(amount));
                effect = TagEffect(effect, statusEffect.Id);

                linkedEffect = linkedEffect == null
                    ? effect
                    : EffectLinkEffects(linkedEffect, effect);
            }

            return linkedEffect;
        }

        public bool HasStatusEffect(uint creature, Type statusEffectClass)
        {
            return _creatureEffects.ContainsKey(creature) &&
                   _creatureEffects[creature].GetAllEffects().Any(x => x.GetType() == statusEffectClass);
        }

        public IStatusEffect GetStatusEffect(uint creature, Type statusEffectClass)
        {
            return _creatureEffects.ContainsKey(creature)
                ? _creatureEffects[creature].GetAllEffects().FirstOrDefault(x => x.GetType() == statusEffectClass)
                : null;
        }

        public IStatusEffect GetStatusEffect(uint creature, Type statusEffectClass, uint source)
        {
            return _creatureEffects.ContainsKey(creature)
                ? _creatureEffects[creature]
                    .GetAllEffects()
                    .FirstOrDefault(x => x.GetType() == statusEffectClass && x.Source == source)
                : null;
        }

        public bool ExtendStatusEffectDuration(
            uint creature,
            Type statusEffectClass,
            uint source,
            float durationSeconds)
        {
            if (!_creatureEffects.TryGetValue(creature, out var creatureEffects) ||
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
                ReplaceNativeStatusEffect(creature, statusEffect, statusEffect.DurationTicks, statusEffect.DurationTicks < 0);
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
            if (!_creatureEffects.TryGetValue(creature, out var creatureEffects) ||
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
                ReplaceNativeStatusEffect(creature, statusEffect, statusEffect.DurationTicks, statusEffect.DurationTicks < 0);
                refreshed = true;
                if (_combatRecorder.IsRecording)
                    _combatRecorder.RecordStatusApplied(source, creature, statusEffect, statusEffect.GetRemainingDurationSeconds(Clock.UtcNow), false, true);
            }

            return refreshed;
        }

        public bool HasStatusEffect<T>(uint creature)
            where T : IStatusEffect
        {
            return HasStatusEffect(creature, typeof(T));
        }

        public bool HasStatusEffect(uint creature, Type statusEffectClass, uint source)
        {
            return _creatureEffects.ContainsKey(creature) &&
                   _creatureEffects[creature].GetAllEffects()
                       .Any(x => x.GetType() == statusEffectClass && x.Source == source);
        }

        public bool HasStatusEffect(uint creature, params Type[] statusEffectClasses)
        {
            return statusEffectClasses.Any(type => HasStatusEffect(creature, type));
        }

        public bool HasStatusEffectCategory(uint creature, StatusEffectCategory category)
        {
            return _creatureEffects.ContainsKey(creature) &&
                   _creatureEffects[creature]
                       .GetAllEffects()
                       .Any(effect => (effect.Categories & category) == category);
        }

        public bool HasStatusEffectCategory(Type statusEffectClass, StatusEffectCategory category)
        {
            if (statusEffectClass == null || category == StatusEffectCategory.None)
                return false;

            return _statusCatalog.TryGetCategories(statusEffectClass, out var categories) &&
                   (categories & category) == category;
        }

        public bool HasCleanseableStatusEffect(uint creature, StatusEffectCleanseType cleanseType)
        {
            return GetCreatureStatusEffects(creature)
                .GetAllEffects()
                .Any(effect => HasCleanseType(effect, cleanseType));
        }

        public void RemoveCleanseableStatusEffects(
            uint creature,
            StatusEffectCleanseType cleanseType,
            bool sendsWornOffMessage = true)
        {
            var effects = StatusEffectSelectors
                .WithCleanseType(GetCreatureStatusEffects(creature).GetAllEffects(), cleanseType)
                .ToList();

            foreach (var effect in effects)
            {
                RemoveStatusEffect(creature, effect.GetType(), effect.Source, sendsWornOffMessage);
            }
        }

        public bool RemoveFirstCleanseableStatusEffect(
            uint creature,
            StatusEffectCleanseType cleanseType,
            bool sendsWornOffMessage = true)
        {
            var effect = StatusEffectSelectors
                .WithCleanseType(GetCreatureStatusEffects(creature).GetAllEffects(), cleanseType)
                .FirstOrDefault();

            if (effect == null)
                return false;

            RemoveStatusEffect(creature, effect.GetType(), effect.Source, sendsWornOffMessage);
            return true;
        }

        public bool RemoveFirstStatusEffect(
            uint creature,
            IEnumerable<Type> statusEffectClasses,
            bool sendsWornOffMessage = true)
        {
            var types = statusEffectClasses?
                .Where(type => type != null)
                .ToHashSet() ?? new HashSet<Type>();

            if (types.Count <= 0)
                return false;

            var effect = GetCreatureStatusEffects(creature)
                .GetAllEffects()
                .FirstOrDefault(effect => types.Contains(effect.GetType()));

            if (effect == null)
                return false;

            RemoveStatusEffect(creature, effect.GetType(), effect.Source, sendsWornOffMessage);
            return true;
        }

        public bool RemoveFirstStatusEffectByCategory(
            uint creature,
            StatusEffectCategory category,
            bool sendsWornOffMessage = true)
        {
            var effect = StatusEffectSelectors
                .WithCategory(GetCreatureStatusEffects(creature).GetAllEffects(), category)
                .FirstOrDefault();

            if (effect == null)
                return false;

            RemoveStatusEffect(creature, effect.GetType(), effect.Source, sendsWornOffMessage);
            return true;
        }

        public bool RemoveFirstBeneficialCombatStatusEffect(
            uint creature,
            bool sendsWornOffMessage = true)
        {
            var effect = StatusEffectSelectors
                .BeneficialInCombat(GetCreatureStatusEffects(creature).GetAllEffects())
                .FirstOrDefault();

            if (effect == null)
                return false;

            RemoveStatusEffect(creature, effect.GetType(), effect.Source, sendsWornOffMessage);
            return true;
        }

        public void ConsumeStatusEffectStat(uint creature, StatType statType)
        {
            GetCreatureStatusEffects(creature).ConsumeStat(statType);
        }

        /// <summary>Sets a stat an effect recalculates while active, keeping the creature's stat total in step.</summary>
        public void SetStatusEffectStat(uint creature, IStatusEffect statusEffect, StatType statType, int value)
        {
            if (GetCreatureStatusEffects(creature).SetStat(statusEffect, statType, value))
                _gui.PublishCharacterSheetRefreshEvent(creature, new StatAdjustmentRefreshEvent());
        }

        public void RemoveStatusEffectsWithStat(
            uint creature,
            StatType statType,
            bool sendsWornOffMessage = true)
        {
            var effects = StatusEffectSelectors
                .WithStat(GetCreatureStatusEffects(creature).GetAllEffects(), statType)
                .ToList();

            foreach (var effect in effects)
            {
                RemoveStatusEffect(creature, effect.GetType(), effect.Source, sendsWornOffMessage);
            }
        }

        public void RemoveStatusEffectFromAllTargetsBySource(
            Type statusEffectClass,
            uint source,
            bool sendsWornOffMessage = true)
        {
            if (statusEffectClass == null || !GetIsObjectValid(source))
                return;

            var targets = _creatureEffects
                .Where(entry => entry.Value
                    .GetAllEffects()
                    .Any(effect => effect.GetType() == statusEffectClass && effect.Source == source))
                .Select(entry => entry.Key)
                .ToList();

            foreach (var target in targets)
            {
                RemoveStatusEffect(target, statusEffectClass, source, sendsWornOffMessage);
            }
        }

        /// <summary>
        /// Removes matching instances from active and logged-out targets without clearing unrelated
        /// effects of the same class. Cached targets lose the matching stat payload before restoration.
        /// </summary>
        /// <param name="source">The creature that granted the effects.</param>
        /// <param name="statusEffectType">The status class, including derived classes, to remove.</param>
        /// <param name="sendsWornOffMessage">Whether active targets receive expiration feedback.</param>
        /// <param name="originatingAbility">
        /// A cached ability definition for exact ownership matching, or null for source-wide cleanup.
        /// Effects with an unknown origin are preserved when an ability is specified.
        /// </param>
        public void RemoveStatusEffectsFromAllTargetsBySource(
            uint source,
            Type statusEffectType,
            bool sendsWornOffMessage = true,
            AbilityDetail originatingAbility = null)
        {
            if (!GetIsObjectValid(source) || statusEffectType == null)
                return;

            var effectsByTarget = _creatureEffects
                .Select(entry => new
                {
                    Target = entry.Key,
                    Effects = GetSourceOwnedStatusEffects(entry.Value.GetAllEffects(), statusEffectType, source, originatingAbility)
                        .Select(effect => effect.Id)
                        .ToList()
                })
                .Where(entry => entry.Effects.Count > 0)
                .ToList();

            foreach (var entry in effectsByTarget)
            {
                foreach (var effectId in entry.Effects)
                    RemoveStatusEffectById(entry.Target, effectId, sendsWornOffMessage, true);
            }

            foreach (var loggedOutEffects in GetLoggedOutEffects())
            {
                var sourceOwnedEffects = GetSourceOwnedStatusEffects(
                    loggedOutEffects.GetAllEffects(), statusEffectType, source, originatingAbility);
                foreach (var effect in sourceOwnedEffects)
                    loggedOutEffects.Remove(effect);
            }
        }

        /// <summary>
        /// Snapshots effects matching their status type, current source, and optional ability origin.
        /// A null requested origin permits source-wide matching; an effect's null origin is never
        /// a wildcard for a specific ability.
        /// </summary>
        public IReadOnlyList<IStatusEffect> GetSourceOwnedStatusEffects(
            IEnumerable<IStatusEffect> effects, Type statusEffectType, uint source, AbilityDetail originatingAbility = null)
        {
            return effects.Where(effect => statusEffectType.IsAssignableFrom(effect.GetType()) && effect.Source == source &&
                (originatingAbility == null || effect.OriginatingAbility == originatingAbility))
                .ToArray();
        }

        public void RemoveStatusEffectsFromAllTargetsWhenSourceExits(uint source)
        {
            if (!GetIsObjectValid(source))
                return;

            var effectsByTarget = _creatureEffects
                .Select(entry => new
                {
                    Target = entry.Key,
                    EffectTypes = entry.Value.GetAllEffects()
                        .Where(effect => effect.Source == source && effect is IRemoveWhenSourceExits)
                        .Select(effect => effect.GetType())
                        .Distinct()
                        .ToList()
                })
                .Where(entry => entry.EffectTypes.Count > 0)
                .ToList();

            foreach (var entry in effectsByTarget)
            {
                foreach (var effectType in entry.EffectTypes)
                    RemoveStatusEffect(entry.Target, effectType, source, false);
            }

            foreach (var loggedOutEffects in GetLoggedOutEffects())
            {
                var sourceOwnedEffects = loggedOutEffects.GetAllEffects()
                    .Where(effect => effect.Source == source && effect is IRemoveWhenSourceExits)
                    .ToList();
                foreach (var effect in sourceOwnedEffects)
                    loggedOutEffects.Remove(effect);
            }
        }

        public void RemoveStatusEffectsWithNegativeStat(
            uint creature,
            StatType statType,
            bool sendsWornOffMessage = true)
        {
            var effects = StatusEffectSelectors
                .WithNegativeStat(GetCreatureStatusEffects(creature).GetAllEffects(), statType)
                .ToList();

            foreach (var effect in effects)
            {
                RemoveStatusEffect(creature, effect.GetType(), effect.Source, sendsWornOffMessage);
            }
        }

        public uint GetStatusEffectSourceWithStat(uint creature, StatType statType)
        {
            var effect = GetCreatureStatusEffects(creature)
                .GetAllEffects()
                .Where(effect =>
                    GetIsObjectValid(effect.Source) &&
                    effect.StatGroup.Stats.TryGetValue(statType, out var value) &&
                    value != 0)
                .OrderByDescending(effect => Math.Abs(effect.StatGroup.Stats[statType]))
                .FirstOrDefault();

            return effect?.Source ?? OBJECT_INVALID;
        }

        public bool HasCleanseType(IStatusEffect effect, StatusEffectCleanseType cleanseType)
        {
            return (effect.CleanseTypes & cleanseType) == cleanseType;
        }

        public void RemoveStatusEffect(
            uint creature,
            Type statusEffectClass,
            bool sendsWornOffMessage = true,
            bool removeNativeEffect = true)
        {
            RemoveStatusEffect(statusEffectClass, creature, OBJECT_INVALID, sendsWornOffMessage, removeNativeEffect);
        }

        public void RemoveStatusEffect(
            uint creature,
            Type statusEffectClass,
            uint source,
            bool sendsWornOffMessage = true,
            bool removeNativeEffect = true)
        {
            RemoveStatusEffect(statusEffectClass, creature, source, sendsWornOffMessage, removeNativeEffect);
        }

        public void RemoveOtherStanceStatuses(
            uint creature,
            Type statusEffectType,
            bool sendsWornOffMessage = false,
            bool removeNativeEffect = true)
        {
            if (_statusCatalog.GetStatusEffectSourceType(statusEffectType) != StatusEffectSourceType.Stance)
                return;

            NotifyStanceChanged(creature);

            RemoveStatusEffectBySourceType(
                creature,
                StatusEffectSourceType.Stance,
                sendsWornOffMessage,
                statusEffectType,
                removeNativeEffect);
        }

        /// <summary>
        /// Enforces command exclusivity for Leadership-style party-buff commands (Press the
        /// Attack, Cleanse Order, Decisive Command): applying one removes any other
        /// <see cref="StatusEffectSourceType.Command"/>-classified effect that the same source
        /// leader previously applied to this target. Effects applied by other leaders, and
        /// non-Command effects such as auras, are left untouched. Newest application wins.
        /// </summary>
        public void RemoveOtherCommandStatuses(
            uint creature,
            Type statusEffectType,
            uint source,
            bool sendsWornOffMessage = false,
            bool removeNativeEffect = true)
        {
            if (_statusCatalog.GetStatusEffectSourceType(statusEffectType) != StatusEffectSourceType.Command)
                return;

            RemoveStatusEffectBySourceType(
                creature,
                StatusEffectSourceType.Command,
                sendsWornOffMessage,
                statusEffectType,
                removeNativeEffect,
                source,
                isReplacement: true);
        }

        public List<Type> GetStatusEffectsFromIcon(uint creature, EffectIconType effectIcon)
        {
            var activeStatusTypes = GetCreatureStatusEffects(creature)
                .GetAllEffects()
                .Where(effect => effect.Icon == effectIcon)
                .Select(effect => effect.GetType())
                .ToList();

            return activeStatusTypes.Count > 0
                ? activeStatusTypes
                : _statusCatalog.GetStatusEffectsFromIcon(effectIcon);
        }

        public int GetEffectDuration(uint creature, params Type[] effectTypes)
        {
            if (!_creatureEffects.ContainsKey(creature))
                return 0;

            var effect = _creatureEffects[creature]
                .GetAllEffects()
                .FirstOrDefault(x => effectTypes.Contains(x.GetType()));

            if (effect == null || effect.DurationTicks < 0)
                return 0;

            var nativeDuration = GetNativeStatusEffectDuration(creature, effect.Id);
            return nativeDuration > 0
                ? nativeDuration
                : (int)Math.Ceiling(effect.DurationTicks * effect.Frequency);
        }

        private int GetNativeStatusEffectDuration(uint creature, string statusEffectId)
        {
            var remaining = 0;

            for (var effect = GetFirstEffect(creature); GetIsEffectValid(effect); effect = GetNextEffect(creature))
            {
                if (GetEffectTag(effect) != statusEffectId)
                    continue;

                remaining = Math.Max(remaining, GetEffectDurationRemaining(effect));
            }

            return remaining;
        }

        private void RemoveStatusEffect(
            Type type,
            uint creature,
            uint source,
            bool sendsWornOffMessage = true,
            bool removeNativeEffect = true,
            bool isReplacement = false)
        {
            if (!_creatureEffects.TryGetValue(creature, out var creatureEffects))
                return;

            // RemoveStatusEffectInstance only emits the worn-off message for the final remaining
            // instance of a given type, so removing every instance here yields exactly one message.
            var statusEffects = creatureEffects.GetAllEffects();
            foreach (var statusEffect in statusEffects)
            {
                if (statusEffect.GetType() != type)
                    continue;

                if (source != OBJECT_INVALID && statusEffect.Source != source)
                    continue;

                RemoveAndProfileStatusEffectInstance(
                    creature,
                    creatureEffects,
                    statusEffect,
                    sendsWornOffMessage,
                    removeNativeEffect,
                    isReplacement);
            }

            RemoveCreatureIfEmpty(creature, creatureEffects, removeNativeEffect);
        }

        public void RemoveStatusEffectById(
            uint creature,
            string statusEffectId,
            bool sendsWornOffMessage = true,
            bool removeNativeEffect = true)
        {
            if (!_creatureEffects.TryGetValue(creature, out var creatureEffects))
            {
                if (removeNativeEffect)
                    RemoveNativeStatusEffect(creature, statusEffectId);

                return;
            }

            var statusEffect = creatureEffects.GetAllEffects().FirstOrDefault(x => x.Id == statusEffectId);
            if (statusEffect == null)
            {
                if (removeNativeEffect)
                    RemoveNativeStatusEffect(creature, statusEffectId);

                return;
            }

            RemoveAndProfileStatusEffectInstance(creature, creatureEffects, statusEffect, sendsWornOffMessage, removeNativeEffect);
            RemoveCreatureIfEmpty(creature, creatureEffects, removeNativeEffect);
        }

        private void RemoveAndProfileStatusEffectInstance(
            uint creature,
            CreatureStatusEffect creatureEffects,
            IStatusEffect statusEffect,
            bool sendsWornOffMessage,
            bool removeNativeEffect,
            bool isReplacement = false)
        {
            var profile = FrameProfiler.Start();
            try
            {
                RemoveStatusEffectInstance(creature, creatureEffects, statusEffect, sendsWornOffMessage, removeNativeEffect, isReplacement);
            }
            finally
            {
                FrameProfiler.Stop(FrameMetric.StatusEffectRemove, profile, statusEffect.Name);
            }
        }

        private void RemoveStatusEffectInstance(
            uint creature,
            CreatureStatusEffect creatureEffects,
            IStatusEffect statusEffect,
            bool sendsWornOffMessage,
            bool removeNativeEffect,
            bool isReplacement = false)
        {
            if (sendsWornOffMessage &&
                statusEffect.SendsWornOffMessage &&
                IsLastInstanceOfType(creatureEffects, statusEffect))
            {
                var effectName = statusEffect.Name;
                string BuildWornOffMessage(uint receiver) =>
                    $"{_playerName.GetDisplayName(receiver, creature)}'s {effectName} effect has worn off.";
                if (StatusEffectRules.ShouldAnnounceNearby(ConcealmentRules.IsConcealed(creature), false))
                    _messaging.SendMessageNearbyToPlayers(creature, BuildWornOffMessage);
                else
                    SendMessageToPC(creature, BuildWornOffMessage(creature));
            }

            if (removeNativeEffect)
                RemoveNativeStatusEffect(creature, statusEffect.Id);

            statusEffect.RemoveEffect(creature, isReplacement);
            creatureEffects.Remove(statusEffect);
            NotifyStatusEffectRemoved(creature, statusEffect);

            if (statusEffect is ILeadershipDamageReductionStatusEffect)
            {
                LeadershipDamageReductionRules.ReconcileLeadershipDamageReduction(creatureEffects);
            }

            if (HasStatAdjustment(statusEffect))
            {
                DelayCommand(0.1f, () => ApplyCreatureMovementRate(creature));
            }

            ScheduleTemporaryHitPointLimitCheckForVitality(creature, statusEffect);

            if (!isReplacement)
            {
                PublishStatusEffectRemovedRefresh(creature);
            }
        }

        /// <summary>
        /// Returns true when the supplied status effect is the only remaining instance of its
        /// runtime type on the creature. Stacking effects (e.g. Suppression) apply many separate
        /// instances that each expire on their own timer; gating the "worn off" message on the
        /// final instance collapses what would otherwise be one message per stack into a single
        /// notification when the effect fully wears off.
        /// </summary>
        private bool IsLastInstanceOfType(CreatureStatusEffect creatureEffects, IStatusEffect statusEffect)
        {
            var type = statusEffect.GetType();
            return creatureEffects.GetAllEffects().Count(effect => effect.GetType() == type) <= 1;
        }

        public void NotifyStatusEffectRemoved(uint creature, IStatusEffect statusEffect)
        {
            if (statusEffect.SourceType == StatusEffectSourceType.Stance)
                NotifyStanceChanged(creature);
            if (statusEffect is IStatusEffectRemovedHandler handler)
                handler.AfterRemoved(creature);
        }

        private bool HasStatAdjustment(IStatusEffect statusEffect)
        {
            return statusEffect.StatGroup.Stats.Any(stat => stat.Value != 0);
        }

        private void PublishStatusEffectReceivedRefresh(uint creature)
        {
            _gui.PublishCharacterSheetRefreshEvent(creature, new StatusEffectReceivedRefreshEvent());
        }

        private void PublishStatusEffectRemovedRefresh(uint creature)
        {
            _gui.PublishCharacterSheetRefreshEvent(creature, new StatusEffectRemovedRefreshEvent());
        }

        private void RemoveNativeStatusEffect(uint creature, string statusEffectId)
        {
            _suppressedStatusEffectRemovalIds.Add(statusEffectId);
            RemoveEffectByTag(creature, statusEffectId);
            _suppressedStatusEffectRemovalIds.Remove(statusEffectId);
        }

        public void RemoveStatusEffect<T>(uint creature)
            where T: IStatusEffect
        {
            var type = typeof(T);
            RemoveStatusEffect(type, creature);
        }

        public void RemoveStatusEffect(Type type, uint creature)
        {
            RemoveStatusEffect(type, creature, OBJECT_INVALID);
        }

        public void RemoveStatusEffectBySourceType(
            uint creature,
            StatusEffectSourceType sourceType,
            bool sendsWornOffMessage = true,
            Type excludedStatusEffectType = null,
            bool removeNativeEffect = true,
            uint filterBySource = OBJECT_INVALID,
            bool isReplacement = false)
        {
            var creatureEffects = GetCreatureStatusEffects(creature);
            var effects = creatureEffects.GetAllBySourceType(sourceType);
            foreach (var effect in effects)
            {
                if (excludedStatusEffectType != null && effect.GetType() == excludedStatusEffectType)
                    continue;

                if (filterBySource != OBJECT_INVALID && effect.Source != filterBySource)
                    continue;

                RemoveStatusEffect(
                    effect.GetType(),
                    creature,
                    effect.Source,
                    sendsWornOffMessage,
                    removeNativeEffect,
                    isReplacement);
            }
        }

        public bool HasEffect(Type type, uint creature)
        {
            if (!_creatureEffects.ContainsKey(creature))
                return false;

            return _creatureEffects[creature].HasEffect(type);
        }

        public bool HasEffect<T>(uint creature)
            where T : IStatusEffect
        {
            return HasEffect(typeof(T), creature);
        }

        private void RemoveStatusEffect(IStatusEffect statusEffect, uint creature, uint source = OBJECT_INVALID)
        {
            RemoveStatusEffect(statusEffect.GetType(), creature, source);
        }

        public T GetStatusEffect<T>(uint creature)
            where T : class, IStatusEffect
        {
            return !_creatureEffects.ContainsKey(creature)
                ? null
                : _creatureEffects[creature].GetEffect<T>();
        }


        /// <summary>
        /// Removes every status effect from a creature. Crafting styles stay unless <paramref name="includeCraftingStyles"/> is true,
        /// which only a full reset (a rebuild, entering space) passes.
        /// </summary>
        public void RemoveAllStatusEffects(uint creature, bool includeCraftingStyles = false)
        {
            if (!_creatureEffects.ContainsKey(creature))
                return;

            var effects = StatusEffectSelectors.Removable(_creatureEffects[creature].GetAllEffects(), includeCraftingStyles).ToList();
            foreach (var effect in effects)
            {
                RemoveStatusEffect(effect.GetType(), creature);
            }
        }

        /// <summary>
        /// Removes a creature from the status effect system entirely
        /// </summary>
        public void RemoveCreature(uint creature, bool removeNativeEffect = true)
        {
            if (_creatureEffects.ContainsKey(creature))
            {
                _creatureEffects.Remove(creature);
            }

            if (removeNativeEffect)
            {
                RemoveEffectByTag(creature, StatusEffectTag);
            }
        }

        private void RemoveCreatureIfEmpty(
            uint creature,
            CreatureStatusEffect effects,
            bool removeNativeEffect = true)
        {
            if (effects.GetAllEffects().Count > 0)
                return;

            RemoveCreature(creature, removeNativeEffect);
        }
        [NWNEventHandler(ScriptName.OnSWLORDamage)]
        public void OnDealtDamage()
        {
            var attacker = OBJECT_SELF;
            var defender = StringToObject(EventsPlugin.GetEventData("DEFENDER"));
            var damage = Convert.ToInt32(EventsPlugin.GetEventData("DAMAGE"));
            var damageTypeText = EventsPlugin.GetEventData("DAMAGE_TYPE");
            var damageType = int.TryParse(damageTypeText, out var parsedDamageType)
                ? (CombatDamageType)parsedDamageType
                : CombatDamageType.Physical;

            NotifyDamageStatusEffects(attacker, defender, damage, damageType);

            var effects = GetCreatureStatusEffects(attacker);

            foreach (var effect in effects.GetAllOnHitEffects())
            {
                effect.OnHitEffect(attacker, defender, damage);
            }
        }

        public void NotifyDamageStatusEffects(
            uint attacker,
            uint defender,
            int damage,
            CombatDamageType damageType,
            CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct)
        {
            if (damage <= 0 || !GetIsObjectValid(attacker) || !GetIsObjectValid(defender))
                return;

            foreach (var effect in GetCreatureStatusEffects(attacker).GetAllEffects())
            {
                effect.OnDamageDealtEffect(attacker, defender, damage, damageType, deliveryType);
            }

            foreach (var effect in GetCreatureStatusEffects(defender).GetAllEffects())
            {
                effect.OnDamageTakenEffect(defender, attacker, damage, damageType, deliveryType);
            }
        }

        /// <summary>
        /// Notifies status effects that must validate their state before an originating hit is
        /// applied. Ordinary damage reactions remain in NotifyDamageStatusEffects so they observe
        /// the defender's post-hit state whenever the damage path supports it.
        /// </summary>
        public void NotifyPreDamageStatusEffects(
            uint attacker,
            uint defender,
            int damage,
            CombatDamageType damageType,
            CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct)
        {
            if (damage <= 0 || !GetIsObjectValid(attacker) || !GetIsObjectValid(defender))
                return;

            foreach (var effect in GetCreatureStatusEffects(defender)
                         .GetAllEffects()
                         .OfType<IPreDamageStatusEffect>()
                         .ToList())
            {
                effect.OnBeforeDamageTaken(defender, attacker, damage, damageType, deliveryType);
            }
            NotifyBeforeDirectDamage(attacker, defender, deliveryType);
        }

        /// <summary>
        /// Notifies limited-attack status effects once per originating hostile attack, including
        /// misses and deflections. Effects exhausted by the attempt are removed immediately so
        /// they cannot affect another attack before the next status-effect tick.
        /// </summary>
        public void NotifyAttackAttemptStatusEffects(
            uint attacker,
            SkillType skillType,
            AbilityImpactSummary abilityImpact = null)
        {
            if (!GetIsObjectValid(attacker))
                return;

            var effects = GetCreatureStatusEffects(attacker)
                .GetAllEffects()
                .OfType<IAttackAttemptStatusEffect>()
                .ToList();

            foreach (var effect in effects)
            {
                effect.OnAttackAttemptedEffect(attacker, skillType, abilityImpact);
            }

            foreach (var effect in effects.Where(effect => effect.IsFlaggedForRemoval))
            {
                RemoveStatusEffect(
                    attacker,
                    effect.GetType(),
                    effect.Source,
                    effect.SendsWornOffMessage);
            }
        }

        /// <summary>
        /// Marks the start of a native attack cycle. Limited attack-timing effects granted
        /// while its already-scheduled rolls resolve are deferred until the outermost cycle ends.
        /// Dual-wield cycles retain this scope across the gap between hands.
        /// </summary>
        public void BeginNativeAttackSwing(uint attacker)
        {
            _nativeAttackSwingDepth.TryGetValue(attacker, out var depth);
            _nativeAttackSwingDepth[attacker] = depth + 1;
        }

        /// <summary>
        /// Discards a destroyed attacker's open native attack cycle and its deferred effects without applying them,
        /// so a reused object ID never starts inside a stale cycle.
        /// </summary>
        public void ForgetDestroyedNativeAttackSwing(uint attacker)
        {
            _nativeAttackSwingDepth.Remove(attacker);
            _deferredNativeAttackStatusEffects.Remove(attacker);
        }

        /// <summary>
        /// Ends (or cancels) a native attack cycle and applies limited attack-timing effects
        /// granted while its precomputed rolls resolved.
        /// </summary>
        public void EndNativeAttackSwing(uint attacker)
        {
            if (!_nativeAttackSwingDepth.TryGetValue(attacker, out var depth))
                return;

            if (depth > 1)
            {
                _nativeAttackSwingDepth[attacker] = depth - 1;
                return;
            }

            _nativeAttackSwingDepth.Remove(attacker);
            if (!_deferredNativeAttackStatusEffects.Remove(attacker, out var deferredEffects))
                return;

            foreach (var applyDeferredEffect in deferredEffects)
            {
                applyDeferredEffect();
            }
        }

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
            if (!IsDeferredNativeAttackStatusEffect(statusEffect, creature))
                return false;

            if (!_deferredNativeAttackStatusEffects.TryGetValue(creature, out var deferredEffects))
            {
                deferredEffects = new List<Action>();
                _deferredNativeAttackStatusEffects[creature] = deferredEffects;
            }

            deferredEffects.Add(() => ApplyAndProfileStatusEffect(
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

        private bool IsDeferredNativeAttackStatusEffect(IStatusEffect statusEffect, uint creature)
        {
            return (statusEffect is ILimitedAttackDelayReductionStatusEffect ||
                    statusEffect is ILimitedAttackNoDelayStatusEffect) &&
                   _nativeAttackSwingDepth.ContainsKey(creature);
        }

        /// <summary>
        /// Gets the attack-delay reduction and earliest remaining charge count supplied by active
        /// limited-attack effects that apply to the requested skill.
        /// </summary>
        public bool TryGetLimitedAttackDelayReduction(
            uint attacker,
            SkillType skillType,
            out int reductionPercent,
            out int remainingAttacks)
        {
            if (IsAttackDelayReductionSuppressed(attacker))
            {
                reductionPercent = 0;
                remainingAttacks = 0;
                return false;
            }

            var effects = GetCreatureStatusEffects(attacker)
                .GetAllEffects()
                .OfType<ILimitedAttackDelayReductionStatusEffect>()
                .Where(effect => effect.AppliesToSkill(skillType) && effect.RemainingAttacks > 0)
                .ToList();

            reductionPercent = effects.Sum(effect => effect.AttackDelayReductionPercent);
            remainingAttacks = effects.Count == 0
                ? 0
                : effects.Min(effect => effect.RemainingAttacks);
            return effects.Count > 0 && reductionPercent > 0;
        }

        /// <summary>
        /// Gets the earliest remaining charge count supplied by active limited no-delay effects
        /// that apply to the requested attack skill.
        /// </summary>
        public bool TryGetLimitedAttackNoDelay(
            uint attacker,
            SkillType skillType,
            out int remainingAttacks)
        {
            if (IsAttackDelayReductionSuppressed(attacker))
            {
                remainingAttacks = 0;
                return false;
            }

            var effects = GetCreatureStatusEffects(attacker)
                .GetAllEffects()
                .OfType<ILimitedAttackNoDelayStatusEffect>()
                .Where(effect => effect.AppliesToSkill(skillType) && effect.RemainingAttacks > 0)
                .ToList();

            remainingAttacks = effects.Count == 0
                ? 0
                : effects.Min(effect => effect.RemainingAttacks);
            return effects.Count > 0;
        }

        public void OnGuardedHit(uint defender, uint attacker, int preventedDamage)
        {
            if (!_creatureEffects.TryGetValue(defender, out var creatureEffects))
                return;

            foreach (var effect in creatureEffects
                         .GetAllEffects()
                         .OfType<IGuardedHitStatusEffect>()
                         .ToList())
            {
                effect.OnGuardedHitEffect(defender, attacker, preventedDamage);
            }
        }

        public virtual void ReapplyFoodHP(uint player)
        {
            RequireStatEffectListeners();
            foreach (var listener in _statEffectListeners)
                listener.OnStatusEffectsRestored(player);
        }

        public virtual void ApplyCreatureMovementRate(uint creature)
        {
            RequireStatEffectListeners();
            foreach (var listener in _statEffectListeners)
                listener.OnStatAdjustingEffectsChanged(creature);
        }

        private void RequireStatEffectListeners()
        {
            if (_statEffectListeners.Count == 0)
                throw new InvalidOperationException("No stat effect listener is registered with the status effects.");
        }

        protected virtual int GetCreatureStatAdjustment(uint creature, StatType stat) => _stat.Value.GetStatAdjustment(creature, stat);

        protected virtual AbilityImpactSummary GetActiveAbilityImpactSummary(uint activator) => _activeImpact.GetActiveAbilityImpactSummary(activator);
        private void NotifyStanceChanged(uint creature)
        {
            RequireMeleeListeners();
            foreach (var listener in _meleeListeners)
                listener.OnStanceChanged(creature);
        }

        private void NotifyBeforeDirectDamage(uint attacker, uint defender, CombatDamageDeliveryType deliveryType)
        {
            RequireMeleeListeners();
            foreach (var listener in _meleeListeners)
                listener.OnBeforeDirectDamage(attacker, defender, deliveryType);
        }

        private void NotifyStatusApplied(uint source, uint creature, StatusEffectCategory categories)
        {
            RequireAppliedListeners();
            foreach (var listener in _appliedListeners)
                listener.OnStatusApplied(source, creature, categories);
        }

        private void NotifyHostileDebuffApplied(uint source, AbilityDetail ability)
        {
            RequireAppliedListeners();
            foreach (var listener in _appliedListeners)
                listener.OnHostileDebuffApplied(source, ability);
        }

        private void NotifyApplicationCompleted(uint source, uint creature, Type statusEffectType, AbilityDetail ability)
        {
            RequireAppliedListeners();
            foreach (var listener in _appliedListeners)
                listener.OnApplicationCompleted(source, creature, statusEffectType, ability);
        }

        private void RequireMeleeListeners()
        {
            if (_meleeListeners.Count == 0)
                throw new InvalidOperationException("No melee listener is registered with the status effects.");
        }

        private void RequireAppliedListeners()
        {
            if (_appliedListeners.Count == 0)
                throw new InvalidOperationException("No applied listener is registered with the status effects.");
        }

        protected virtual bool IsAttackDelayReductionSuppressed(uint creature) => GetCreatureStatAdjustment(creature, StatType.AttackDelayReductionSuppressed) > 0;

        protected virtual void ScheduleTemporaryHitPointLimitCheck(uint target) => TemporaryHitPointEffects.ScheduleLimitCheck(target);

        // A native read of the object's applied effects.
        protected virtual bool MayHaveEffectTag(uint creature, string tag) => AppliedEffects.MayHaveTag(creature, tag);
    }
}
