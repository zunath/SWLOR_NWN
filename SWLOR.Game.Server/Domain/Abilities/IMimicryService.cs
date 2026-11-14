using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// Mimicry lets players with the Combat Analyzer perk witness enemy creatures using their innate
    /// techniques in combat. When the source creature dies, witnesses roll a chance to learn the
    /// technique permanently. Learned techniques can then be equipped (within a slot budget) as
    /// ordinary active ability feats, scaled off the player's stats and the Technique Potency perk.
    /// </summary>
    public interface IMimicryService
    {
        /// <summary>
        /// Caches technique lookups. Runs on OnModuleCacheAfter so Ability's cache (built on
        /// OnModuleCacheBefore) has already populated every AbilityDetail, including the
        /// Mimicry-specific fields set by AbilityBuilder.MimicryTechnique.
        /// </summary>
        void CacheData();

        /// <summary>
        /// Returns each equipped trait's stat payload with the parameters declared by that trait.
        /// </summary>
        IReadOnlyList<StatAdjustmentSource> GetStatSources(uint creature, StatType payloadStat);

        /// <summary>
        /// Sums equipped trait adjustments and the elemental-resonance set bonus. These belong
        /// to the loadout rather than status effects that could be cleared on death.
        /// </summary>
        int GetStatBonus(uint creature, StatType stat);

        /// <summary>
        /// Sums the resistance adjustments contributed by a creature's equipped Mimicry traits.
        /// </summary>
        int GetResistanceBonus(uint creature, ResistanceType resistance);

        /// <summary>
        /// Returns the cached feat.2da ICON resref for a technique, resolved once at cache time.
        /// Empty string if the feat is not a registered technique.
        /// </summary>
        string GetTechniqueIcon(FeatType feat);

        /// <summary>
        /// Returns authored technique text cached at boot, without the hotbar TLK headers.
        /// Empty string if the feat has no cached description.
        /// </summary>
        string GetTechniqueDescription(FeatType feat);

        /// <summary>
        /// Called from the ability-use pipeline whenever a non-player creature uses a registered feat.
        /// If the feat maps to a technique, every nearby player who has the Combat Analyzer perk and
        /// hasn't already learned the technique has the witness recorded (used at the creature's death
        /// to roll for learning). Sends a one-time floating text per (npc, player, technique).
        /// </summary>
        /// <param name="activator">The creature that used the ability.</param>
        /// <param name="npcFeat">The feat that was used.</param>
        void OnCreatureAbilityUsed(uint activator, FeatType npcFeat);

        /// <summary>
        /// Pays observation XP only after the normal kill payout has validated participation,
        /// survival, area, and distance. Consuming the credit prevents repeat event payouts.
        /// </summary>
        void CombatPointXPDistributed();

        /// <summary>
        /// Observation grants 5% of rank-relative kill XP, capped at 30 before normal penalties.
        /// Trivial enemies and mastered Mimicry grant no observation XP.
        /// </summary>
        int CalculateAnalysisXP(int npcLevel, int mimicryRank);

        void ClearAnalysisOnPlayerExit();

        /// <summary>
        /// When a creature dies, every player who witnessed one of its techniques gets a chance to
        /// learn it permanently. The witness cache entry for this creature is always cleared afterward.
        /// </summary>
        void OnCreatureDeath();

        /// <summary>
        /// Computes the percent chance to learn a witnessed technique when the source creature dies.
        /// Scales off Mimicry skill rank above the technique's individual requirement, the Pattern Recognition
        /// perk, and the player's Perception attribute (each point above <see cref="PerceptionLearnChanceBaseline"/>
        /// adds <see cref="LearnChancePerPerceptionPoint"/> percent, rewarding perceptive characters).
        /// The result is clamped to <see cref="MaxLearnChancePercent"/>.
        /// </summary>
        int CalculateLearnChance(int skillRank, int requiredSkillRank, int patternRecognitionLevel, int perception);

        /// <summary>
        /// Grants every registered technique to a player as learned, bypassing the witness/roll flow.
        /// Intended for tester and DM tooling (e.g. the learn-all chat command). Does not equip the
        /// techniques or grant the Combat Analyzer perk; the player still equips them through the
        /// Techniques window. Returns the number of techniques newly added to the player's learned set.
        /// </summary>
        int GrantAllTechniques(uint player);

        /// <summary>
        /// Returns the maximum number of technique slots available to a player.
        /// Zero if the player does not have the Combat Analyzer perk.
        /// </summary>
        int GetMaxSlots(uint player);

        /// <summary>
        /// Returns the maximum number of technique slots available to a player's database record.
        /// </summary>
        int GetMaxSlots(Player dbPlayer);

        /// <summary>
        /// Damage-type loadout set bonus ("elemental resonance"): equipping multiple active
        /// techniques sharing a damage type grants scaling technique potency. Each damage type with
        /// at least two equipped techniques contributes (count - 1) * <see cref="ResonancePotencyPerTechnique"/>
        /// percent, summed across types and capped at <see cref="ResonancePotencyCap"/>. Passive
        /// traits have no damage element and do not contribute.
        /// </summary>
        int GetSetBonusPotency(Player dbPlayer);

        /// <summary>
        /// Returns the number of technique slots currently used by a player's equipped techniques.
        /// </summary>
        int GetUsedSlots(uint player);

        /// <summary>
        /// Returns the number of technique slots currently used by a player database record's equipped techniques.
        /// </summary>
        int GetUsedSlots(Player dbPlayer);

        /// <summary>
        /// Determines whether a player can equip a given technique right now.
        /// </summary>
        bool CanEquip(uint player, FeatType feat, out string error);

        /// <summary>
        /// Determines whether a player can equip a given technique right now, using an
        /// already-fetched player database record to avoid duplicate round trips.
        /// </summary>
        bool CanEquip(uint player, Player dbPlayer, FeatType feat, out string error);

        /// <summary>
        /// Equips a learned technique, granting the underlying feat and adding it to the player's hotbar.
        /// </summary>
        bool EquipTechnique(uint player, FeatType feat);

        /// <summary>
        /// Unequips a technique, removing the underlying feat and any hotbar slot referencing it.
        /// </summary>
        bool UnequipTechnique(uint player, FeatType feat);

        /// <summary>
        /// Unequips techniques that exceed the player's current Mimicry rank, then removes the
        /// newest-equipped techniques until the remaining loadout fits the current slot budget.
        /// Used after progression changes and on login to keep persisted loadouts valid.
        /// </summary>
        void EnforceSlotBudget(uint player);

        /// <summary>
        /// Enforces Mimicry rank requirements and the slot budget against an already-fetched player
        /// database record, avoiding a duplicate round trip when the caller has one in hand.
        /// </summary>
        void EnforceSlotBudget(uint player, Player dbPlayer);

        /// <summary>
        /// Immediately removes equipped techniques whose rank requirement is no longer met when
        /// Mimicry loses a rank. This must run for every Mimicry decay, including rank losses that
        /// do not cross a perk requirement and therefore do not trigger a perk refund callback.
        /// </summary>
        void OnMimicrySkillDecay();

        /// <summary>
        /// Unequips every equipped technique. Used when the Combat Analyzer perk is refunded.
        /// </summary>
        void UnequipAllTechniques(uint player);

        /// <summary>
        /// On login, re-grants every equipped technique's feat (in case it was lost, e.g. a fresh
        /// character load) and enforces rank and slot limits in case progression changed since logout.
        /// </summary>
        void OnPlayerLogin();

        /// <summary>Validates the entire draft before changing any equipped state.</summary>
        bool ValidateLoadout(Player dbPlayer, IReadOnlyCollection<FeatType> techniques, out string error);

        void CaptureLoadoutHotbar(uint player, MimicryLoadout loadout);

        bool ApplyLoadout(uint player, string loadoutId, IReadOnlyCollection<FeatType> techniques, out string error);

        /// <summary>
        /// Returns every technique a player has learned, alongside its cached ability detail.
        /// </summary>
        List<(FeatType Feat, AbilityDetail Detail)> GetLearnedTechniques(uint player);

        /// <summary>
        /// Returns every technique a player database record has learned, alongside its cached ability detail.
        /// </summary>
        List<(FeatType Feat, AbilityDetail Detail)> GetLearnedTechniques(Player dbPlayer);

        /// <summary>
        /// Returns every technique a player currently has equipped, alongside its cached ability detail.
        /// </summary>
        List<(FeatType Feat, AbilityDetail Detail)> GetEquippedTechniques(uint player);

        /// <summary>
        /// Returns every technique a player database record has equipped, alongside its cached ability detail.
        /// </summary>
        List<(FeatType Feat, AbilityDetail Detail)> GetEquippedTechniques(Player dbPlayer);

        /// <summary>
        /// Returns true when a technique feat is present in a player's equipped loadout.
        /// </summary>
        bool IsTechniqueEquipped(uint player, FeatType feat);

        /// <summary>
        /// Returns true when a technique feat is present in an already-fetched player record's
        /// equipped loadout.
        /// </summary>
        bool IsTechniqueEquipped(Player dbPlayer, FeatType feat);

        /// <summary>
        /// Retrieves the cached ability detail for a technique feat, or null if the feat isn't a technique.
        /// </summary>
        AbilityDetail GetTechniqueDetail(FeatType feat);

        /// <summary>
        /// Returns true if the given feat is a registered Mimicry technique.
        /// </summary>
        bool IsTechnique(FeatType feat);

        /// <summary>
        /// Returns the total number of registered techniques.
        /// </summary>
        int TechniqueCount { get; }
    }
}
