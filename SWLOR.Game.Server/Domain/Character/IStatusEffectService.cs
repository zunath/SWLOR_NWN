using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// Status effects: applying, refreshing and removing them, what a creature currently has, and the module events that
    /// drive them. Runs on every attack and effect tick; the reads allocate nothing.
    /// </summary>
    public interface IStatusEffectService
    {
        /// <summary>
        /// Registers who is told when a player's status effects are restored and when a stat-adjusting effect changes
        /// (<see cref="IStatEffectListener"/>). <c>CreatureStatService</c> registers itself at boot; notifying with no listener
        /// registered throws, so a missing registration fails at once.
        /// </summary>
        void RegisterStatEffectListener(IStatEffectListener listener);

        /// <summary>
        /// Registers who is told about stance removal and about direct hits about to land (<see cref="IStatusEffectMeleeListener"/>).
        /// Registering the same listener twice keeps one entry.
        /// </summary>
        void RegisterMeleeListener(IStatusEffectMeleeListener listener);

        /// <summary>
        /// Registers who is told when a status effect is applied or refreshed (<see cref="IStatusEffectAppliedListener"/>), in the
        /// order they register. Registering the same listener twice keeps one entry.
        /// </summary>
        void RegisterAppliedListener(IStatusEffectAppliedListener listener);

        /// <summary>
        /// When a player enters the server, apply the NWN effect system
        /// </summary>
        void OnPlayerEnter();

        /// <summary>
        /// When a player exits the server, hold their status effects until they return.
        /// </summary>
        void OnPlayerExit();

        /// <summary>
        /// Saves the stances and toggles of everyone online or holding effects from an earlier logout. Those effects are only kept in
        /// memory, so without this a server restart loses them.
        /// </summary>
        void SaveTogglesForRestart();

        void ClearStatusEffectsOnDeath(uint player);

        /// <summary>
        /// Handle when a status effect is applied (called by NWN script)
        /// </summary>
        void OnApplyStatusEffect();

        /// <summary>
        /// Handle when a status effect is removed (called by NWN script)
        /// </summary>
        void OnRemoveStatusEffect();

        /// <summary>
        /// Handle status effect interval processing (called by NWN script)
        /// </summary>
        void OnStatusEffectInterval();

        void OnApplyNWNStatusEffect(uint player);

        void OnRemoveNWNStatusEffect(uint player);

        void OnNWNStatusEffectInterval(uint creature);

        CreatureStatusEffect GetCreatureStatusEffects(uint creature);

        IReadOnlyList<StatAdjustmentSource> GetStatSources(uint creature, StatType payloadStat);

        int GetStatAdjustment(uint creature, StatType stat);

        bool HasAnyActiveEffect(uint creature, IReadOnlySet<Type> effectTypes);

        bool HasAnyActiveEffect(uint creature, StatusEffectSourceType sourceType);

        void ApplyPermanentStatusEffect<T>(uint source, uint creature)
            where T: IStatusEffect;

        void ApplyPermanentStatusEffect(Type type, uint source, uint creature);

        /// <summary>
        /// Restarts an active effect's duration exactly as re-applying the same effect from the same
        /// source would, without replacing it or repeating its application message. For effects whose
        /// re-application changes nothing else, such as Embattled with unchanged stacks.
        /// </summary>
        bool RestartStatusEffectDuration(
            uint creature,
            Type statusEffectClass,
            uint source,
            float durationSeconds);

        bool ApplyStatusEffect<T>(
            uint source,
            uint creature,
            float durationSeconds,
            ResistanceType resistanceOverride = ResistanceType.Invalid)
            where T: IStatusEffect;

        bool ApplyStatusEffect<T>(
            uint source,
            uint creature,
            float durationSeconds,
            CombatDamageType sourceDamageType)
            where T : IStatusEffect;

        bool ApplyStatusEffect(
            uint source,
            uint creature,
            IStatusEffect statusEffect,
            float durationSeconds,
            ResistanceType resistanceOverride = ResistanceType.Invalid);

        bool ApplyStatusEffect(
            uint source,
            uint creature,
            IStatusEffect statusEffect,
            float durationSeconds,
            CombatDamageType sourceDamageType);

        bool ApplyStatusEffect(
            uint source,
            uint creature,
            Type statusEffectClass,
            float durationSeconds,
            ResistanceType resistanceOverride = ResistanceType.Invalid);

        bool ApplyStatusEffect(
            uint source,
            uint creature,
            Type statusEffectClass,
            float durationSeconds,
            CombatDamageType sourceDamageType,
            Type replacedStatusEffectType = null);

        bool HasStatusEffect(uint creature, Type statusEffectClass);

        IStatusEffect GetStatusEffect(uint creature, Type statusEffectClass);

        IStatusEffect GetStatusEffect(uint creature, Type statusEffectClass, uint source);

        bool ExtendStatusEffectDuration(
            uint creature,
            Type statusEffectClass,
            uint source,
            float durationSeconds);

        bool RefreshStatusEffectDuration(
            uint creature,
            Type statusEffectClass,
            uint source,
            float durationSeconds,
            ResistanceType resistanceOverride = ResistanceType.Invalid,
            CombatDamageType sourceDamageType = CombatDamageType.Invalid);

        bool HasStatusEffect<T>(uint creature)
            where T : IStatusEffect;

        bool HasStatusEffect(uint creature, Type statusEffectClass, uint source);

        bool HasStatusEffect(uint creature, params Type[] statusEffectClasses);

        bool HasStatusEffectCategory(uint creature, StatusEffectCategory category);

        bool HasStatusEffectCategory(Type statusEffectClass, StatusEffectCategory category);

        bool HasCleanseableStatusEffect(uint creature, StatusEffectCleanseType cleanseType);

        void RemoveCleanseableStatusEffects(
            uint creature,
            StatusEffectCleanseType cleanseType,
            bool sendsWornOffMessage = true);

        bool RemoveFirstCleanseableStatusEffect(
            uint creature,
            StatusEffectCleanseType cleanseType,
            bool sendsWornOffMessage = true);

        bool RemoveFirstStatusEffect(
            uint creature,
            IEnumerable<Type> statusEffectClasses,
            bool sendsWornOffMessage = true);

        bool RemoveFirstStatusEffectByCategory(
            uint creature,
            StatusEffectCategory category,
            bool sendsWornOffMessage = true);

        bool RemoveFirstBeneficialCombatStatusEffect(
            uint creature,
            bool sendsWornOffMessage = true);

        void ConsumeStatusEffectStat(uint creature, StatType statType);

        /// <summary>Sets a stat an effect recalculates while active, keeping the creature's stat total in step.</summary>
        void SetStatusEffectStat(uint creature, IStatusEffect statusEffect, StatType statType, int value);

        void RemoveStatusEffectsWithStat(
            uint creature,
            StatType statType,
            bool sendsWornOffMessage = true);

        void RemoveStatusEffectFromAllTargetsBySource(
            Type statusEffectClass,
            uint source,
            bool sendsWornOffMessage = true);

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
        void RemoveStatusEffectsFromAllTargetsBySource(
            uint source,
            Type statusEffectType,
            bool sendsWornOffMessage = true,
            AbilityDetail originatingAbility = null);

        /// <summary>
        /// Snapshots effects matching their status type, current source, and optional ability origin.
        /// A null requested origin permits source-wide matching; an effect's null origin is never
        /// a wildcard for a specific ability.
        /// </summary>
        IReadOnlyList<IStatusEffect> GetSourceOwnedStatusEffects(
            IEnumerable<IStatusEffect> effects, Type statusEffectType, uint source, AbilityDetail originatingAbility = null);

        void RemoveStatusEffectsWithNegativeStat(
            uint creature,
            StatType statType,
            bool sendsWornOffMessage = true);

        uint GetStatusEffectSourceWithStat(uint creature, StatType statType);

        bool HasCleanseType(IStatusEffect effect, StatusEffectCleanseType cleanseType);

        void RemoveStatusEffect(
            uint creature,
            Type statusEffectClass,
            bool sendsWornOffMessage = true,
            bool removeNativeEffect = true);

        void RemoveStatusEffect(
            uint creature,
            Type statusEffectClass,
            uint source,
            bool sendsWornOffMessage = true,
            bool removeNativeEffect = true);

        void RemoveOtherStanceStatuses(
            uint creature,
            Type statusEffectType,
            bool sendsWornOffMessage = false,
            bool removeNativeEffect = true);

        /// <summary>
        /// Enforces command exclusivity for Leadership-style party-buff commands (Press the
        /// Attack, Cleanse Order, Decisive Command): applying one removes any other
        /// <see cref="StatusEffectSourceType.Command"/>-classified effect that the same source
        /// leader previously applied to this target. Effects applied by other leaders, and
        /// non-Command effects such as auras, are left untouched. Newest application wins.
        /// </summary>
        void RemoveOtherCommandStatuses(
            uint creature,
            Type statusEffectType,
            uint source,
            bool sendsWornOffMessage = false,
            bool removeNativeEffect = true);

        List<Type> GetStatusEffectsFromIcon(uint creature, EffectIconType effectIcon);

        int GetEffectDuration(uint creature, params Type[] effectTypes);

        void RemoveStatusEffect<T>(uint creature)
            where T: IStatusEffect;

        void RemoveStatusEffect(Type type, uint creature);

        void RemoveStatusEffectBySourceType(
            uint creature,
            StatusEffectSourceType sourceType,
            bool sendsWornOffMessage = true,
            Type excludedStatusEffectType = null,
            bool removeNativeEffect = true,
            uint filterBySource = OBJECT_INVALID,
            bool isReplacement = false);

        bool HasEffect(Type type, uint creature);

        bool HasEffect<T>(uint creature)
            where T : IStatusEffect;

        T GetStatusEffect<T>(uint creature)
            where T : class, IStatusEffect;

        /// <summary>
        /// Removes every status effect from a creature. Crafting styles stay unless <paramref name="includeCraftingStyles"/> is true,
        /// which only a full reset (a rebuild, entering space) passes.
        /// </summary>
        void RemoveAllStatusEffects(uint creature, bool includeCraftingStyles = false);

        /// <summary>
        /// Removes a creature from the status effect system entirely
        /// </summary>
        void RemoveCreature(uint creature, bool removeNativeEffect = true);

        void OnDealtDamage();

        void NotifyDamageStatusEffects(
            uint attacker,
            uint defender,
            int damage,
            CombatDamageType damageType,
            CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct);

        /// <summary>
        /// Notifies status effects that must validate their state before an originating hit is
        /// applied. Ordinary damage reactions remain in NotifyDamageStatusEffects so they observe
        /// the defender's post-hit state whenever the damage path supports it.
        /// </summary>
        void NotifyPreDamageStatusEffects(
            uint attacker,
            uint defender,
            int damage,
            CombatDamageType damageType,
            CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct);

        /// <summary>
        /// Notifies limited-attack status effects once per originating hostile attack, including
        /// misses and deflections. Effects exhausted by the attempt are removed immediately so
        /// they cannot affect another attack before the next status-effect tick.
        /// </summary>
        void NotifyAttackAttemptStatusEffects(
            uint attacker,
            SkillType skillType,
            AbilityImpactSummary abilityImpact = null);

        /// <summary>
        /// Marks the start of a native attack cycle. Limited attack-timing effects granted
        /// while its already-scheduled rolls resolve are deferred until the outermost cycle ends.
        /// Dual-wield cycles retain this scope across the gap between hands.
        /// </summary>
        void BeginNativeAttackSwing(uint attacker);

        /// <summary>
        /// Discards a destroyed attacker's open native attack cycle and its deferred effects without applying them,
        /// so a reused object ID never starts inside a stale cycle.
        /// </summary>
        void ForgetDestroyedNativeAttackSwing(uint attacker);

        /// <summary>
        /// Ends (or cancels) a native attack cycle and applies limited attack-timing effects
        /// granted while its precomputed rolls resolved.
        /// </summary>
        void EndNativeAttackSwing(uint attacker);

        /// <summary>
        /// Gets the attack-delay reduction and earliest remaining charge count supplied by active
        /// limited-attack effects that apply to the requested skill.
        /// </summary>
        bool TryGetLimitedAttackDelayReduction(
            uint attacker,
            SkillType skillType,
            out int reductionPercent,
            out int remainingAttacks);

        /// <summary>
        /// Gets the earliest remaining charge count supplied by active limited no-delay effects
        /// that apply to the requested attack skill.
        /// </summary>
        bool TryGetLimitedAttackNoDelay(
            uint attacker,
            SkillType skillType,
            out int remainingAttacks);

        void OnGuardedHit(uint defender, uint attacker, int preventedDamage);
    }
}
