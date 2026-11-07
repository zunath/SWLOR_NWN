using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Status effects: applying, refreshing and removing them, and what a creature currently has. The logic lives in
    /// <see cref="IStatusEffectService"/>; the event handlers stay here and every member forwards to the container's
    /// instance. <see cref="StatusEffectRules"/> holds the pure duration rules, for code that has no container.
    /// </summary>
    public static class StatusEffect
    {
        public const int MaximumHardCrowdControlDurationSeconds = StatusEffectRules.MaximumHardCrowdControlDurationSeconds;

        private static IStatusEffectService Instance => FacadeAccessor.Resolve<IStatusEffectService>();

        /// <summary>
        /// When the module caches, status effects will be discovered and cached.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheData() => Instance.CacheData();

        /// <summary>
        /// Creates a fresh instance of a registered status effect so a caller can set per-application options before applying it.
        /// </summary>
        public static IStatusEffect CreateStatusEffect(Type statusEffectClass)
            => Instance.CreateStatusEffect(statusEffectClass);

        /// <summary>
        /// When a player enters the server, apply the NWN effect system
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void OnPlayerEnter() => Instance.OnPlayerEnter();

        /// <summary>
        /// When a player exits the server, hold their status effects until they return.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void OnPlayerExit() => Instance.OnPlayerExit();

        /// <summary>
        /// Saves the stances and toggles of everyone online or holding effects from an earlier logout. Those effects are only kept in
        /// memory, so without this a server restart loses them.
        /// </summary>
        public static void SaveTogglesForRestart() => Instance.SaveTogglesForRestart();

        public static void ClearStatusEffectsOnDeath(uint player) => Instance.ClearStatusEffectsOnDeath(player);

        /// <summary>
        /// Handle when a status effect is applied (called by NWN script)
        /// </summary>
        [NWNEventHandler(ScriptName.OnApplyStatusEffect)]
        public static void OnApplyStatusEffect() => Instance.OnApplyStatusEffect();

        /// <summary>
        /// Handle when a status effect is removed (called by NWN script)
        /// </summary>
        [NWNEventHandler(ScriptName.OnRemoveStatusEffect)]
        public static void OnRemoveStatusEffect() => Instance.OnRemoveStatusEffect();

        /// <summary>
        /// Handle status effect interval processing (called by NWN script)
        /// </summary>
        [NWNEventHandler(ScriptName.OnStatusEffectInterval)]
        public static void OnStatusEffectInterval() => Instance.OnStatusEffectInterval();

        public static void OnApplyNWNStatusEffect(uint player) => Instance.OnApplyNWNStatusEffect(player);

        public static void OnRemoveNWNStatusEffect(uint player) => Instance.OnRemoveNWNStatusEffect(player);

        public static void OnNWNStatusEffectInterval(uint creature)
            => Instance.OnNWNStatusEffectInterval(creature);

        public static CreatureStatusEffect GetCreatureStatusEffects(uint creature)
            => Instance.GetCreatureStatusEffects(creature);

        public static IReadOnlyList<StatAdjustmentSource> GetStatSources(uint creature, StatType payloadStat)
            => Instance.GetStatSources(creature, payloadStat);

        public static int GetStatAdjustment(uint creature, StatType stat)
            => Instance.GetStatAdjustment(creature, stat);

        public static bool HasAnyActiveEffect(uint creature, IReadOnlySet<Type> effectTypes)
            => Instance.HasAnyActiveEffect(creature, effectTypes);

        public static bool HasAnyActiveEffect(uint creature, StatusEffectSourceType sourceType)
            => Instance.HasAnyActiveEffect(creature, sourceType);

        public static void ApplyPermanentStatusEffect<T>(uint source, uint creature)
            where T: IStatusEffect
            => Instance.ApplyPermanentStatusEffect<T>(source, creature);

        public static void ApplyPermanentStatusEffect(Type type, uint source, uint creature)
            => Instance.ApplyPermanentStatusEffect(type, source, creature);

        /// <summary>
        /// Application and worn-off messages go to every nearby player only while neither the bearer
        /// nor the applying creature is hidden; otherwise they would reveal who is there.
        /// </summary>
        public static bool ShouldAnnounceNearby(bool bearerHidden, bool sourceHidden)
            => Instance.ShouldAnnounceNearby(bearerHidden, sourceHidden);

        /// <summary>
        /// Restarts an active effect's duration exactly as re-applying the same effect from the same
        /// source would, without replacing it or repeating its application message. For effects whose
        /// re-application changes nothing else, such as Embattled with unchanged stacks.
        /// </summary>
        public static bool RestartStatusEffectDuration(
            uint creature,
            Type statusEffectClass,
            uint source,
            float durationSeconds)
            => Instance.RestartStatusEffectDuration(creature, statusEffectClass, source, durationSeconds);

        public static string BuildDurationResistanceMessage(
            ResistanceType resistanceType,
            string effectName,
            int originalDurationTicks,
            int adjustedDurationTicks,
            float frequency)
            => Instance.BuildDurationResistanceMessage(resistanceType, effectName, originalDurationTicks, adjustedDurationTicks, frequency);

        public static int GetOutgoingDurationPercentAdjustment(StatusEffectCategory categories, Func<StatType, int> getStat)
            => Instance.GetOutgoingDurationPercentAdjustment(categories, getStat);

        /// <summary>A conversion must not move the original control expiration or its immunity window.</summary>
        public static int ClampConvertedControlDurationTicks(int requestedTicks, float frequency, float remainingSeconds)
            => Instance.ClampConvertedControlDurationTicks(requestedTicks, frequency, remainingSeconds);

        /// <summary>
        /// Apply the final control budget after outgoing duration bonuses and resistance.
        /// Self-applied system penalties, soft debuffs and permanent effects are unaffected.
        /// </summary>
        /// <param name="isPlayerAgainstPlayer">A player or a player's pet applied it to another player, so it lasts half as long.</param>
        public static int ClampHardCrowdControlDurationTicks(
            StatusEffectCategory categories, int ticks, float frequency, bool isSelfApplied = false, bool isPlayerAgainstPlayer = false)
            => Instance.ClampHardCrowdControlDurationTicks(categories, ticks, frequency, isSelfApplied, isPlayerAgainstPlayer);

        public static float CalculateAdjustedStatusDurationSeconds(float seconds, int percentAdjustment)
            => Instance.CalculateAdjustedStatusDurationSeconds(seconds, percentAdjustment);

        public static DateTime? GetRefreshTickAnchor(IStatusEffect incoming, IEnumerable<IStatusEffect> active, uint source)
            => Instance.GetRefreshTickAnchor(incoming, active, source);

        public static bool ApplyStatusEffect<T>(
            uint source,
            uint creature,
            float durationSeconds,
            ResistanceType resistanceOverride = ResistanceType.Invalid)
            where T: IStatusEffect
            => Instance.ApplyStatusEffect<T>(source, creature, durationSeconds, resistanceOverride);

        public static bool ApplyStatusEffect<T>(
            uint source,
            uint creature,
            float durationSeconds,
            CombatDamageType sourceDamageType)
            where T : IStatusEffect
            => Instance.ApplyStatusEffect<T>(source, creature, durationSeconds, sourceDamageType);

        public static bool ApplyStatusEffect(
            uint source,
            uint creature,
            IStatusEffect statusEffect,
            float durationSeconds,
            ResistanceType resistanceOverride = ResistanceType.Invalid)
            => Instance.ApplyStatusEffect(source, creature, statusEffect, durationSeconds, resistanceOverride);

        public static bool ApplyStatusEffect(
            uint source,
            uint creature,
            IStatusEffect statusEffect,
            float durationSeconds,
            CombatDamageType sourceDamageType)
            => Instance.ApplyStatusEffect(source, creature, statusEffect, durationSeconds, sourceDamageType);

        public static bool ApplyStatusEffect(
            uint source,
            uint creature,
            Type statusEffectClass,
            float durationSeconds,
            ResistanceType resistanceOverride = ResistanceType.Invalid)
            => Instance.ApplyStatusEffect(source, creature, statusEffectClass, durationSeconds, resistanceOverride);

        public static bool ApplyStatusEffect(
            uint source,
            uint creature,
            Type statusEffectClass,
            float durationSeconds,
            CombatDamageType sourceDamageType,
            Type replacedStatusEffectType = null)
            => Instance.ApplyStatusEffect(source, creature, statusEffectClass, durationSeconds, sourceDamageType, replacedStatusEffectType);

        public static bool HasStatusEffectDefinition(Type statusEffectClass)
            => Instance.HasStatusEffectDefinition(statusEffectClass);

        public static bool HasStatusEffect(uint creature, Type statusEffectClass)
            => Instance.HasStatusEffect(creature, statusEffectClass);

        public static IStatusEffect GetStatusEffect(uint creature, Type statusEffectClass)
            => Instance.GetStatusEffect(creature, statusEffectClass);

        public static IStatusEffect GetStatusEffect(uint creature, Type statusEffectClass, uint source)
            => Instance.GetStatusEffect(creature, statusEffectClass, source);

        public static bool ExtendStatusEffectDuration(
            uint creature,
            Type statusEffectClass,
            uint source,
            float durationSeconds)
            => Instance.ExtendStatusEffectDuration(creature, statusEffectClass, source, durationSeconds);

        public static bool RefreshStatusEffectDuration(
            uint creature,
            Type statusEffectClass,
            uint source,
            float durationSeconds,
            ResistanceType resistanceOverride = ResistanceType.Invalid,
            CombatDamageType sourceDamageType = CombatDamageType.Invalid)
            => Instance.RefreshStatusEffectDuration(creature, statusEffectClass, source, durationSeconds, resistanceOverride, sourceDamageType);

        public static bool HasStatusEffect<T>(uint creature)
            where T : IStatusEffect
            => Instance.HasStatusEffect<T>(creature);

        public static bool HasStatusEffect(uint creature, Type statusEffectClass, uint source)
            => Instance.HasStatusEffect(creature, statusEffectClass, source);

        public static bool HasStatusEffect(uint creature, params Type[] statusEffectClasses)
            => Instance.HasStatusEffect(creature, statusEffectClasses);

        public static bool HasStatusEffectCategory(uint creature, StatusEffectCategory category)
            => Instance.HasStatusEffectCategory(creature, category);

        public static bool HasStatusEffectCategory(Type statusEffectClass, StatusEffectCategory category)
            => Instance.HasStatusEffectCategory(statusEffectClass, category);

        public static bool HasCleanseableStatusEffect(uint creature, StatusEffectCleanseType cleanseType)
            => Instance.HasCleanseableStatusEffect(creature, cleanseType);

        public static void RemoveCleanseableStatusEffects(
            uint creature,
            StatusEffectCleanseType cleanseType,
            bool sendsWornOffMessage = true)
            => Instance.RemoveCleanseableStatusEffects(creature, cleanseType, sendsWornOffMessage);

        public static bool RemoveFirstCleanseableStatusEffect(
            uint creature,
            StatusEffectCleanseType cleanseType,
            bool sendsWornOffMessage = true)
            => Instance.RemoveFirstCleanseableStatusEffect(creature, cleanseType, sendsWornOffMessage);

        public static bool RemoveFirstStatusEffect(
            uint creature,
            IEnumerable<Type> statusEffectClasses,
            bool sendsWornOffMessage = true)
            => Instance.RemoveFirstStatusEffect(creature, statusEffectClasses, sendsWornOffMessage);

        public static bool RemoveFirstStatusEffectByCategory(
            uint creature,
            StatusEffectCategory category,
            bool sendsWornOffMessage = true)
            => Instance.RemoveFirstStatusEffectByCategory(creature, category, sendsWornOffMessage);

        public static bool RemoveFirstBeneficialCombatStatusEffect(
            uint creature,
            bool sendsWornOffMessage = true)
            => Instance.RemoveFirstBeneficialCombatStatusEffect(creature, sendsWornOffMessage);

        public static void ConsumeStatusEffectStat(uint creature, StatType statType)
            => Instance.ConsumeStatusEffectStat(creature, statType);

        /// <summary>Sets a stat an effect recalculates while active, keeping the creature's stat total in step.</summary>
        public static void SetStatusEffectStat(uint creature, IStatusEffect statusEffect, StatType statType, int value)
            => Instance.SetStatusEffectStat(creature, statusEffect, statType, value);

        public static void RemoveStatusEffectsWithStat(
            uint creature,
            StatType statType,
            bool sendsWornOffMessage = true)
            => Instance.RemoveStatusEffectsWithStat(creature, statType, sendsWornOffMessage);

        public static void RemoveStatusEffectFromAllTargetsBySource(
            Type statusEffectClass,
            uint source,
            bool sendsWornOffMessage = true)
            => Instance.RemoveStatusEffectFromAllTargetsBySource(statusEffectClass, source, sendsWornOffMessage);

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
        public static void RemoveStatusEffectsFromAllTargetsBySource(
            uint source,
            Type statusEffectType,
            bool sendsWornOffMessage = true,
            AbilityDetail originatingAbility = null)
            => Instance.RemoveStatusEffectsFromAllTargetsBySource(source, statusEffectType, sendsWornOffMessage, originatingAbility);

        /// <summary>
        /// Snapshots effects matching their status type, current source, and optional ability origin.
        /// A null requested origin permits source-wide matching; an effect's null origin is never
        /// a wildcard for a specific ability.
        /// </summary>
        public static IReadOnlyList<IStatusEffect> GetSourceOwnedStatusEffects(
            IEnumerable<IStatusEffect> effects, Type statusEffectType, uint source, AbilityDetail originatingAbility = null)
            => Instance.GetSourceOwnedStatusEffects(effects, statusEffectType, source, originatingAbility);

        public static void RemoveStatusEffectsWithNegativeStat(
            uint creature,
            StatType statType,
            bool sendsWornOffMessage = true)
            => Instance.RemoveStatusEffectsWithNegativeStat(creature, statType, sendsWornOffMessage);

        public static uint GetStatusEffectSourceWithStat(uint creature, StatType statType)
            => Instance.GetStatusEffectSourceWithStat(creature, statType);

        public static bool HasCleanseType(IStatusEffect effect, StatusEffectCleanseType cleanseType)
            => Instance.HasCleanseType(effect, cleanseType);

        public static void RemoveStatusEffect(
            uint creature,
            Type statusEffectClass,
            bool sendsWornOffMessage = true,
            bool removeNativeEffect = true)
            => Instance.RemoveStatusEffect(creature, statusEffectClass, sendsWornOffMessage, removeNativeEffect);

        public static void RemoveStatusEffect(
            uint creature,
            Type statusEffectClass,
            uint source,
            bool sendsWornOffMessage = true,
            bool removeNativeEffect = true)
            => Instance.RemoveStatusEffect(creature, statusEffectClass, source, sendsWornOffMessage, removeNativeEffect);

        public static string GetStatusEffectName(Type statusEffectClass)
            => Instance.GetStatusEffectName(statusEffectClass);

        public static StatusEffectSourceType GetStatusEffectSourceType(Type statusEffectClass)
            => Instance.GetStatusEffectSourceType(statusEffectClass);

        public static void RemoveOtherStanceStatuses(
            uint creature,
            Type statusEffectType,
            bool sendsWornOffMessage = false,
            bool removeNativeEffect = true)
            => Instance.RemoveOtherStanceStatuses(creature, statusEffectType, sendsWornOffMessage, removeNativeEffect);

        /// <summary>
        /// Enforces command exclusivity for Leadership-style party-buff commands (Press the
        /// Attack, Cleanse Order, Decisive Command): applying one removes any other
        /// <see cref="StatusEffectSourceType.Command"/>-classified effect that the same source
        /// leader previously applied to this target. Effects applied by other leaders, and
        /// non-Command effects such as auras, are left untouched. Newest application wins.
        /// </summary>
        public static void RemoveOtherCommandStatuses(
            uint creature,
            Type statusEffectType,
            uint source,
            bool sendsWornOffMessage = false,
            bool removeNativeEffect = true)
            => Instance.RemoveOtherCommandStatuses(creature, statusEffectType, source, sendsWornOffMessage, removeNativeEffect);

        public static List<Type> GetStatusEffectsFromIcon(EffectIconType effectIcon)
            => Instance.GetStatusEffectsFromIcon(effectIcon);

        public static List<Type> GetStatusEffectsFromIcon(uint creature, EffectIconType effectIcon)
            => Instance.GetStatusEffectsFromIcon(creature, effectIcon);

        public static int GetEffectDuration(uint creature, params Type[] effectTypes)
            => Instance.GetEffectDuration(creature, effectTypes);

        public static EffectTypeScript GetEffectTypeFromIcon(EffectIconType effectIcon)
            => Instance.GetEffectTypeFromIcon(effectIcon);

        public static AbilityType GetAbilityTypeBuffed(EffectIconType effectIcon)
            => Instance.GetAbilityTypeBuffed(effectIcon);

        public static void RemoveStatusEffect<T>(uint creature)
            where T: IStatusEffect
            => Instance.RemoveStatusEffect<T>(creature);

        public static void RemoveStatusEffect(Type type, uint creature)
            => Instance.RemoveStatusEffect(type, creature);

        public static void RemoveStatusEffectBySourceType(
            uint creature,
            StatusEffectSourceType sourceType,
            bool sendsWornOffMessage = true,
            Type excludedStatusEffectType = null,
            bool removeNativeEffect = true,
            uint filterBySource = OBJECT_INVALID,
            bool isReplacement = false)
            => Instance.RemoveStatusEffectBySourceType(creature, sourceType, sendsWornOffMessage, excludedStatusEffectType, removeNativeEffect, filterBySource, isReplacement);

        public static bool HasEffect(Type type, uint creature) => Instance.HasEffect(type, creature);

        public static bool HasEffect<T>(uint creature)
            where T : IStatusEffect
            => Instance.HasEffect<T>(creature);

        public static T GetStatusEffect<T>(uint creature)
            where T : class, IStatusEffect
            => Instance.GetStatusEffect<T>(creature);

        /// <summary>
        /// Removes every status effect from a creature. Crafting styles stay unless <paramref name="includeCraftingStyles"/> is true,
        /// which only a full reset (a rebuild, entering space) passes.
        /// </summary>
        public static void RemoveAllStatusEffects(uint creature, bool includeCraftingStyles = false)
            => Instance.RemoveAllStatusEffects(creature, includeCraftingStyles);

        /// <summary>
        /// Removes a creature from the status effect system entirely
        /// </summary>
        public static void RemoveCreature(uint creature, bool removeNativeEffect = true)
            => Instance.RemoveCreature(creature, removeNativeEffect);

        [NWNEventHandler(ScriptName.OnSWLORDamage)]
        public static void OnDealtDamage() => Instance.OnDealtDamage();

        public static void NotifyDamageStatusEffects(
            uint attacker,
            uint defender,
            int damage,
            CombatDamageType damageType,
            CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct)
            => Instance.NotifyDamageStatusEffects(attacker, defender, damage, damageType, deliveryType);

        /// <summary>
        /// Notifies status effects that must validate their state before an originating hit is
        /// applied. Ordinary damage reactions remain in NotifyDamageStatusEffects so they observe
        /// the defender's post-hit state whenever the damage path supports it.
        /// </summary>
        public static void NotifyPreDamageStatusEffects(
            uint attacker,
            uint defender,
            int damage,
            CombatDamageType damageType,
            CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct)
            => Instance.NotifyPreDamageStatusEffects(attacker, defender, damage, damageType, deliveryType);

        /// <summary>
        /// Notifies limited-attack status effects once per originating hostile attack, including
        /// misses and deflections. Effects exhausted by the attempt are removed immediately so
        /// they cannot affect another attack before the next status-effect tick.
        /// </summary>
        public static void NotifyAttackAttemptStatusEffects(
            uint attacker,
            SkillType skillType,
            AbilityImpactSummary abilityImpact = null)
            => Instance.NotifyAttackAttemptStatusEffects(attacker, skillType, abilityImpact);

        /// <summary>
        /// Marks the start of a native attack cycle. Limited attack-timing effects granted
        /// while its already-scheduled rolls resolve are deferred until the outermost cycle ends.
        /// Dual-wield cycles retain this scope across the gap between hands.
        /// </summary>
        public static void BeginNativeAttackSwing(uint attacker) => Instance.BeginNativeAttackSwing(attacker);

        /// <summary>
        /// Discards a destroyed attacker's open native attack cycle and its deferred effects without applying them,
        /// so a reused object ID never starts inside a stale cycle.
        /// </summary>
        public static void ForgetDestroyedNativeAttackSwing(uint attacker)
            => Instance.ForgetDestroyedNativeAttackSwing(attacker);

        /// <summary>
        /// Ends (or cancels) a native attack cycle and applies limited attack-timing effects
        /// granted while its precomputed rolls resolved.
        /// </summary>
        public static void EndNativeAttackSwing(uint attacker) => Instance.EndNativeAttackSwing(attacker);

        /// <summary>
        /// Gets the attack-delay reduction and earliest remaining charge count supplied by active
        /// limited-attack effects that apply to the requested skill.
        /// </summary>
        public static bool TryGetLimitedAttackDelayReduction(
            uint attacker,
            SkillType skillType,
            out int reductionPercent,
            out int remainingAttacks)
            => Instance.TryGetLimitedAttackDelayReduction(attacker, skillType, out reductionPercent, out remainingAttacks);

        /// <summary>
        /// Gets the earliest remaining charge count supplied by active limited no-delay effects
        /// that apply to the requested attack skill.
        /// </summary>
        public static bool TryGetLimitedAttackNoDelay(
            uint attacker,
            SkillType skillType,
            out int remainingAttacks)
            => Instance.TryGetLimitedAttackNoDelay(attacker, skillType, out remainingAttacks);

        public static void OnGuardedHit(uint defender, uint attacker, int preventedDamage)
            => Instance.OnGuardedHit(defender, attacker, preventedDamage);
    }
}
