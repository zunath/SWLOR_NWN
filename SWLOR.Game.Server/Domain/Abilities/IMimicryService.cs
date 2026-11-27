using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
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
        /// Reads a technique straight from the technique table. The key is not normalised the way
        /// <see cref="GetTechniqueDetail"/> and <see cref="IsTechnique"/> normalise theirs.
        /// </summary>
        bool TryGetTechnique(FeatType feat, out AbilityDetail detail);

        /// <summary>
        /// Reads a technique straight from the technique table; throws <see cref="KeyNotFoundException"/> for a feat
        /// that is not a technique, as an indexer on the table does.
        /// </summary>
        AbilityDetail GetTechnique(FeatType feat);

        /// <summary>
        /// Finds the technique a creature's own feat teaches, and its ability detail.
        /// Returns false when the feat teaches nothing or the technique is not registered.
        /// </summary>
        bool TryGetTechniqueForNpcFeat(FeatType npcFeat, out FeatType technique, out AbilityDetail detail);

        /// <summary>
        /// Every registered technique with its ability detail, read straight from the technique table without a copy.
        /// </summary>
        IEnumerable<KeyValuePair<FeatType, AbilityDetail>> EnumerateTechniques();

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
