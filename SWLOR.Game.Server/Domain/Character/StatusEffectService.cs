using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// Status effects, part 1 of the status effect split: the per-creature trackers, what a creature has (the reads), the removal
    /// of status effects and the worn-off messages. The logic that used to be static in the facade <c>StatusEffect</c> is moved
    /// here unchanged; static callers reach it through <c>GameServices.StatusEffect</c>. The hit point limit check of the feature code
    /// sits behind a one-line <c>protected virtual</c> method whose body is the original call. Applying, durations, the native
    /// effect, the combat events and the session are the other parts (<see cref="IStatusEffectApplicationService"/>,
    /// <see cref="IStatusEffectDurationService"/>, <see cref="IStatusEffectNativeService"/>,
    /// <see cref="IStatusEffectCombatEventService"/>, <see cref="IStatusEffectSessionService"/>). The pure duration rules live in
    /// <see cref="StatusEffectRules"/>, for code that has no container.
    /// </summary>
    public class StatusEffectService : IStatusEffectService
    {
        private const string StatusEffectTag = "STATUS_EFFECT";

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
        private readonly IStatusEffectCatalogService _statusCatalog;
        private readonly IMessagingService _messaging;
        private readonly IPlayerNameService _playerName;
        private readonly IGuiRefreshPublisher _gui;

        public StatusEffectService(
            IStatusEffectCatalogService statusCatalog,
            IMessagingService messaging,
            IPlayerNameService playerName,
            IGuiRefreshPublisher gui)
        {
            _statusCatalog = statusCatalog;
            _messaging = messaging;
            _playerName = playerName;
            _gui = gui;
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
            RequireLoggedOutSources();
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

        public CreatureStatusEffect EnsureCreatureStatusEffectTracker(uint creature)
        {
            if (!_creatureEffects.TryGetValue(creature, out var effects))
            {
                effects = new CreatureStatusEffect();
                _creatureEffects[creature] = effects;
            }

            return effects;
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

        public void RemoveStatusEffectForReplacement(Type type, uint creature, uint source)
        {
            RemoveStatusEffect(type, creature, source, false, true, true);
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
                AssignCommand(GetModule(), () => DelayCommand(0.1f, () => ApplyCreatureMovementRate(creature)));
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

        public bool HasStatAdjustment(IStatusEffect statusEffect)
        {
            return statusEffect.StatGroup.Stats.Any(stat => stat.Value != 0);
        }

        public void PublishStatusEffectReceivedRefresh(uint creature)
        {
            _gui.PublishCharacterSheetRefreshEvent(creature, new StatusEffectReceivedRefreshEvent());
        }

        private void PublishStatusEffectRemovedRefresh(uint creature)
        {
            _gui.PublishCharacterSheetRefreshEvent(creature, new StatusEffectRemovedRefreshEvent());
        }

        public void RemoveNativeStatusEffect(uint creature, string statusEffectId)
        {
            _suppressedStatusEffectRemovalIds.Add(statusEffectId);
            RemoveEffectByTag(creature, statusEffectId);
            _suppressedStatusEffectRemovalIds.Remove(statusEffectId);
        }

        public bool IsRemovalSuppressed(string statusEffectId)
        {
            return _suppressedStatusEffectRemovalIds.Contains(statusEffectId);
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


        private void NotifyStanceChanged(uint creature)
        {
            RequireMeleeListeners();
            foreach (var listener in _meleeListeners)
                listener.OnStanceChanged(creature);
        }

        public void NotifyBeforeDirectDamage(uint attacker, uint defender, CombatDamageDeliveryType deliveryType)
        {
            RequireMeleeListeners();
            foreach (var listener in _meleeListeners)
                listener.OnBeforeDirectDamage(attacker, defender, deliveryType);
        }

        public void NotifyStatusApplied(uint source, uint creature, StatusEffectCategory categories)
        {
            RequireAppliedListeners();
            foreach (var listener in _appliedListeners)
                listener.OnStatusApplied(source, creature, categories);
        }

        public void NotifyHostileDebuffApplied(uint source, AbilityDetail ability)
        {
            RequireAppliedListeners();
            foreach (var listener in _appliedListeners)
                listener.OnHostileDebuffApplied(source, ability);
        }

        public void NotifyApplicationCompleted(uint source, uint creature, Type statusEffectType, AbilityDetail ability)
        {
            RequireAppliedListeners();
            foreach (var listener in _appliedListeners)
                listener.OnApplicationCompleted(source, creature, statusEffectType, ability);
        }

        private void RequireLoggedOutSources()
        {
            if (_loggedOutSources.Count == 0)
                throw new InvalidOperationException("No logged-out holder is registered with the status effects.");
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

        // Native Vitality effects change maximum HP outside the HP budget setters.
        public void ScheduleTemporaryHitPointLimitCheckForVitality(uint creature, IStatusEffect statusEffect)
        {
            if (statusEffect.StatGroup.Abilities.TryGetValue(AbilityType.Vitality, out var vitality) && vitality != 0)
                ScheduleTemporaryHitPointLimitCheck(creature);
        }

        protected virtual void ScheduleTemporaryHitPointLimitCheck(uint target) => TemporaryHitPointEffects.ScheduleLimitCheck(target);
    }
}
