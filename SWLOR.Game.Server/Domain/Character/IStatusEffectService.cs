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
        /// Registers who holds the effects of players who logged out (<see cref="ILoggedOutStatusEffectSource"/>), so a removal by
        /// source reaches them. Registering the same holder twice keeps one entry.
        /// </summary>
        void RegisterLoggedOutSource(ILoggedOutStatusEffectSource source);

        /// <summary>The creatures that have a status effect tracker.</summary>
        IReadOnlyCollection<uint> GetTrackedCreatures();

        /// <summary>Finds a creature's tracker; false when the creature has none (<see cref="GetCreatureStatusEffects"/> answers an empty one).</summary>
        bool TryGetCreatureStatusEffects(uint creature, out CreatureStatusEffect effects);

        /// <summary>Gives a creature the tracker of effects it already holds, as a player returning from logout gets theirs back.</summary>
        void TrackCreatureStatusEffects(uint creature, CreatureStatusEffect effects);

        /// <summary>Tells the stat effect listeners that a player's status effects were restored, so the food bonus returns.</summary>
        void ReapplyFoodHP(uint player);

        /// <summary>Tells the stat effect listeners that the effects adjusting a creature's stats changed, so its movement rate is recomputed.</summary>
        void ApplyCreatureMovementRate(uint creature);

        /// <summary>Removes the effects that reach other targets from a source that is leaving, from every tracked creature and every logged-out player.</summary>
        void RemoveStatusEffectsFromAllTargetsWhenSourceExits(uint source);

        /// <summary>Removes the effects that do not persist through a logout.</summary>
        void RemoveNonPersistentStatusEffects(uint creature, CreatureStatusEffect effects);

        /// <summary>Removes the native effects the status effects of a creature carry.</summary>
        void RemoveTrackedNWNEffects(uint creature, CreatureStatusEffect effects);

        /// <summary>Removes one status effect by its id, with the worn-off message when asked.</summary>
        void RemoveStatusEffectById(
            uint creature,
            string statusEffectId,
            bool sendsWornOffMessage = true,
            bool removeNativeEffect = true);

        /// <summary>Tells the listeners and the effect's own handler that a status effect was removed.</summary>
        void NotifyStatusEffectRemoved(uint creature, IStatusEffect statusEffect);

        /// <summary>The tracker of a creature's status effects, created when it has none.</summary>
        CreatureStatusEffect EnsureCreatureStatusEffectTracker(uint creature);

        /// <summary>Removes every instance of a type, from one source or any (<c>OBJECT_INVALID</c>), because an application replaces it: no worn-off message, the native effect removed.</summary>
        void RemoveStatusEffectForReplacement(Type type, uint creature, uint source);

        /// <summary>Tells the melee listeners a direct hit is about to land, so the stand-ins and techniques validate their state.</summary>
        void NotifyBeforeDirectDamage(uint attacker, uint defender, CombatDamageDeliveryType deliveryType);

        /// <summary>Removes the native effect with this tag from a creature, without the removal script settling the status effect (the native part replaces and removes effects through it).</summary>
        void RemoveNativeStatusEffect(uint creature, string statusEffectId);

        /// <summary>Whether the removal of the native effect with this tag is in progress and the remove script is to leave the status effect alone.</summary>
        bool IsRemovalSuppressed(string statusEffectId);

        /// <summary>Checks the temporary hit point limit of a creature when an effect adjusts its Vitality, as the native effects and the removal do.</summary>
        void ScheduleTemporaryHitPointLimitCheckForVitality(uint creature, IStatusEffect statusEffect);

        /// <summary>Whether an effect adjusts any stat.</summary>
        bool HasStatAdjustment(IStatusEffect statusEffect);

        /// <summary>Refreshes the character sheet of a creature that received a status effect.</summary>
        void PublishStatusEffectReceivedRefresh(uint creature);

        /// <summary>Tells the applied listeners an effect was applied, so the stamina drain and the like run.</summary>
        void NotifyStatusApplied(uint source, uint creature, StatusEffectCategory categories);

        /// <summary>Tells the applied listeners a hostile debuff was applied, so the party buff runs.</summary>
        void NotifyHostileDebuffApplied(uint source, AbilityDetail ability);

        /// <summary>Tells the applied listeners an application finished, so the capstone proof counts it.</summary>
        void NotifyApplicationCompleted(uint source, uint creature, Type statusEffectType, AbilityDetail ability);

        void ClearStatusEffectsOnDeath(uint player);

        CreatureStatusEffect GetCreatureStatusEffects(uint creature);

        IReadOnlyList<StatAdjustmentSource> GetStatSources(uint creature, StatType payloadStat);

        int GetStatAdjustment(uint creature, StatType stat);

        bool HasAnyActiveEffect(uint creature, IReadOnlySet<Type> effectTypes);

        bool HasAnyActiveEffect(uint creature, StatusEffectSourceType sourceType);

        bool HasStatusEffect(uint creature, Type statusEffectClass);

        IStatusEffect GetStatusEffect(uint creature, Type statusEffectClass);

        IStatusEffect GetStatusEffect(uint creature, Type statusEffectClass, uint source);

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

    }
}
