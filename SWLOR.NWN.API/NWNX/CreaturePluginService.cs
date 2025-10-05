using System.Numerics;
using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWNX.Model;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWNX
{
    /// <summary>
    /// Provides comprehensive creature management functionality including feat management, ability scores,
    /// spell casting, combat statistics, special abilities, and advanced creature properties.
    /// This plugin allows for detailed manipulation of creature characteristics and behaviors.
    /// </summary>
    public class CreaturePluginService : ICreaturePluginService
    {
        /// <inheritdoc/>
        public void AddFeat(uint creature, FeatType feat)
        {
            global::NWN.Core.NWNX.CreaturePlugin.AddFeat(creature, (int)feat);
        }

        /// <inheritdoc/>
        public void AddFeatByLevel(uint creature, FeatType feat, int level)
        {
            global::NWN.Core.NWNX.CreaturePlugin.AddFeatByLevel(creature, (int)feat, level);
        }

        /// <inheritdoc/>
        public void RemoveFeat(uint creature, FeatType feat)
        {
            global::NWN.Core.NWNX.CreaturePlugin.RemoveFeat(creature, (int)feat);
        }

        /// <inheritdoc/>
        public bool GetKnowsFeat(uint creature, FeatType feat)
        {
            var result = global::NWN.Core.NWNX.CreaturePlugin.GetKnowsFeat(creature, (int)feat);
            return result != 0;
        }

        /// <inheritdoc/>
        public int GetFeatCountByLevel(uint creature, int level)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetFeatCountByLevel(creature, level);
        }

        /// <inheritdoc/>
        public FeatType GetFeatByLevel(uint creature, int level, int index)
        {
            var result = global::NWN.Core.NWNX.CreaturePlugin.GetFeatByLevel(creature, level, index);
            return (FeatType)result;
        }

        /// <inheritdoc/>
        public int GetFeatCount(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetFeatCount(creature);
        }

        /// <inheritdoc/>
        public FeatType GetFeatByIndex(uint creature, int index)
        {
            var result = global::NWN.Core.NWNX.CreaturePlugin.GetFeatByIndex(creature, index);
            return (FeatType)result;
        }

        /// <inheritdoc/>
        public bool GetMeetsFeatRequirements(uint creature, FeatType feat)
        {
            var result = global::NWN.Core.NWNX.CreaturePlugin.GetMeetsFeatRequirements(creature, (int)feat);
            return result != 0;
        }

        /// <inheritdoc/>
        public SpecialAbilitySlot GetSpecialAbility(uint creature, int index)
        {
            var coreAbility = global::NWN.Core.NWNX.CreaturePlugin.GetSpecialAbility(creature, index);
            return new SpecialAbilitySlot
            {
                Level = coreAbility.level,
                Ready = coreAbility.ready,
                ID = coreAbility.id
            };
        }

        /// <inheritdoc/>
        public int GetSpecialAbilityCount(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetSpecialAbilityCount(creature);
        }

        /// <inheritdoc/>
        public void AddSpecialAbility(uint creature, SpecialAbilitySlot ability)
        {
            var coreAbility = new global::NWN.Core.NWNX.SpecialAbility
            {
                id = ability.ID,
                ready = ability.Ready,
                level = ability.Level
            };
            global::NWN.Core.NWNX.CreaturePlugin.AddSpecialAbility(creature, coreAbility);
        }

        /// <inheritdoc/>
        public void RemoveSpecialAbility(uint creature, int index)
        {
            global::NWN.Core.NWNX.CreaturePlugin.RemoveSpecialAbility(creature, index);
        }

        /// <inheritdoc/>
        public void SetSpecialAbility(uint creature, int index, SpecialAbilitySlot ability)
        {
            var coreAbility = new global::NWN.Core.NWNX.SpecialAbility
            {
                id = ability.ID,
                ready = ability.Ready,
                level = ability.Level
            };
            global::NWN.Core.NWNX.CreaturePlugin.SetSpecialAbility(creature, index, coreAbility);
        }

        /// <inheritdoc/>
        public ClassType GetClassByLevel(uint creature, int level)
        {
            var result = global::NWN.Core.NWNX.CreaturePlugin.GetClassByLevel(creature, level);
            return (ClassType)result;
        }

        /// <summary>
        /// Sets the base armor class (AC) for the creature.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="ac">The base AC value to set. Must be a non-negative integer.</param>
        /// <remarks>
        /// This sets the creature's base armor class, which forms the foundation for their total AC calculation.
        /// The base AC is modified by armor, dexterity, and other factors to determine the final AC.
        /// Higher values indicate better armor protection.
        /// </remarks>
        public void SetBaseAC(uint creature, int ac)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetBaseAC(creature, ac);
        }

        /// <inheritdoc/>
        public int GetBaseAC(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetBaseAC(creature);
        }

        /// <inheritdoc/>
        public void SetRawAbilityScore(uint creature, AbilityType ability, int value)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetRawAbilityScore(creature, (int)ability, value);
        }

        /// <summary>
        /// Retrieves the raw ability score of the creature.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="ability">The ability type to query. See AbilityType enum for available abilities.</param>
        /// <returns>The creature's raw ability score value.</returns>
        /// <remarks>
        /// This returns the creature's raw ability score without racial bonuses or penalties applied.
        /// Raw ability scores are the base values before any racial or other modifications.
        /// Use SetRawAbilityScore() to modify this value.
        /// </remarks>
        public int GetRawAbilityScore(uint creature, AbilityType ability)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetRawAbilityScore(creature, (int)ability);
        }

        /// <inheritdoc/>
        public void ModifyRawAbilityScore(uint creature, AbilityType ability, int modifier)
        {
            global::NWN.Core.NWNX.CreaturePlugin.ModifyRawAbilityScore(creature, (int)ability, modifier);
        }

        /// <inheritdoc/>
        public int GetPrePolymorphAbilityScore(uint creature, AbilityType ability)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetPrePolymorphAbilityScore(creature, (int)ability);
        }

        /// <summary>
        /// Retrieves the remaining spell slots for the creature's innate casting ability.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="classId">The class ID to check. See ClassType enum for available classes.</param>
        /// <param name="level">The spell level to check. Must be between 0 and 9.</param>
        /// <returns>The number of remaining spell slots for the specified class and level.</returns>
        /// <remarks>
        /// This returns the number of spell slots the creature has remaining for casting spells.
        /// Spell slots are consumed when spells are cast and restored through rest or other means.
        /// Use SetRemainingSpellSlots() to modify this value.
        /// </remarks>
        public int GetRemainingSpellSlots(uint creature, ClassType classId, int level)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetRemainingSpellSlots(creature, (int)classId, level);
        }

        /// <inheritdoc/>
        public void SetRemainingSpellSlots(uint creature, ClassType classId, int level, int slots)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetRemainingSpellSlots(creature, (int)classId, level, slots);
        }

        /// <inheritdoc/>
        public void RemoveKnownSpell(uint creature, ClassType classId, int level, int spellId)
        {
            global::NWN.Core.NWNX.CreaturePlugin.RemoveKnownSpell(creature, (int)classId, level, spellId);
        }

        /// <summary>
        /// Adds a spell to the creature's spellbook for a specific class.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="classId">The class ID to modify. See ClassType enum for available classes.</param>
        /// <param name="level">The spell level to modify. Must be between 0 and 9.</param>
        /// <param name="spellId">The spell ID to add. Must be a valid spell ID.</param>
        /// <remarks>
        /// This adds the specified spell to the creature's known spells for the given class and level.
        /// The creature will be able to cast this spell if they have available spell slots.
        /// Use RemoveKnownSpell() to remove spells from the creature's spellbook.
        /// </remarks>
        public void AddKnownSpell(uint creature, ClassType classId, int level, int spellId)
        {
            global::NWN.Core.NWNX.CreaturePlugin.AddKnownSpell(creature, (int)classId, level, spellId);
        }

        /// <inheritdoc/>
        public int GetMaxSpellSlots(uint creature, ClassType classId, int level)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetMaxSpellSlots(creature, (int)classId, level);
        }

        /// <inheritdoc/>
        public int GetMaxHitPointsByLevel(uint creature, int level)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetMaxHitPointsByLevel(creature, level);
        }

        /// <summary>
        /// Sets the maximum hit points for the creature at a specific level.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="level">The level to modify. Must be a positive integer.</param>
        /// <param name="value">The maximum hit points to set for the specified level. Must be non-negative.</param>
        /// <remarks>
        /// This sets the maximum hit points the creature gained at the specified level.
        /// This affects the creature's total maximum hit points.
        /// The value should be reasonable for the creature's level and class.
        /// </remarks>
        public void SetMaxHitPointsByLevel(uint creature, int level, int value)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetMaxHitPointsByLevel(creature, level, value);
        }

        /// <inheritdoc/>
        public void SetMovementRate(uint creature, MovementRateType rate)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetMovementRate(creature, (int)rate);
        }

        /// <inheritdoc/>
        public float GetMovementRateFactor(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetMovementRateFactor(creature);
        }

        /// <summary>
        /// Sets the creature's current movement rate factor.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="factor">The movement rate factor to set. Base value is 1.0.</param>
        /// <remarks>
        /// This sets the creature's movement rate factor, which multiplies their base movement rate.
        /// A factor of 1.0 represents normal movement speed.
        /// Factors greater than 1.0 increase movement speed, while factors less than 1.0 decrease it.
        /// This affects all movement actions including walking, running, and combat movement.
        /// </remarks>
        public void SetMovementRateFactor(uint creature, float factor)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetMovementRateFactor(creature, factor);
        }

        /// <inheritdoc/>
        public void SetAlignmentGoodEvil(uint creature, int value)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetAlignmentGoodEvil(creature, value);
        }

        /// <inheritdoc/>
        public void SetAlignmentLawChaos(uint creature, int value)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetAlignmentLawChaos(creature, value);
        }

        /// <inheritdoc/>
        public void SetSkillRank(uint creature, NWNSkillType skill, int rank)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetSkillRank(creature, (int)skill, rank);
        }

        /// <inheritdoc/>
        public void SetClassByPosition(uint creature, int position, ClassType classId, bool updateLevels = true)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetClassByPosition(creature, position, (int)classId, updateLevels ? 1 : 0);
        }

        /// <inheritdoc/>
        public void SetBaseAttackBonus(uint creature, int bab)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetBaseAttackBonus(creature, bab);
        }

        /// <summary>
        /// Retrieves the creature's current number of attacks per round.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="bBaseAPR">If true, returns the base attacks per round based on BAB and equipped weapons, ignoring overrides from SetBaseAttackBonus() builtin function.</param>
        /// <returns>The number of attacks per round the creature can make.</returns>
        /// <remarks>
        /// This returns the creature's current attacks per round, which determines how many times they can attack in a single round.
        /// The value is based on the creature's base attack bonus and equipped weapons.
        /// If bBaseAPR is true, it returns the calculated value ignoring any overrides.
        /// </remarks>
        public int GetAttacksPerRound(uint creature, bool bBaseAPR)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetAttacksPerRound(creature, bBaseAPR ? 1 : 0);
        }

        /// <inheritdoc/>
        public void RestoreFeats(uint creature)
        {
            global::NWN.Core.NWNX.CreaturePlugin.RestoreFeats(creature);
        }

        /// <inheritdoc/>
        public void RestoreSpecialAbilities(uint creature)
        {
            global::NWN.Core.NWNX.CreaturePlugin.RestoreSpecialAbilities(creature);
        }

        /// <inheritdoc/>
        public void RestoreItems(uint creature)
        {
            global::NWN.Core.NWNX.CreaturePlugin.RestoreItems(creature);
        }

        /// <inheritdoc/>
        public void SetSize(uint creature, CreatureSizeType creatureSize)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetSize(creature, (int)creatureSize);
        }

        /// <inheritdoc/>
        public int GetSkillPointsRemaining(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetSkillPointsRemaining(creature);
        }

        /// <summary>
        /// Sets the creature's remaining unspent skill points.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="skillpoints">The number of skill points to set. Must be non-negative.</param>
        /// <remarks>
        /// This sets the number of skill points the creature has available to spend on skills.
        /// Skill points are typically gained through level advancement and can be spent to improve skill ranks.
        /// The value should be reasonable for the creature's level and class.
        /// </remarks>
        public void SetSkillPointsRemaining(uint creature, int skillpoints)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetSkillPointsRemaining(creature, skillpoints);
        }

        /// <inheritdoc/>
        public void SetRacialType(uint creature, RacialType racialtype)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetRacialType(creature, (int)racialtype);
        }

        /// <inheritdoc/>
        public MovementType GetMovementType(uint creature)
        {
            var result = global::NWN.Core.NWNX.CreaturePlugin.GetMovementType(creature);
            return (MovementType)result;
        }

        /// <summary>
        /// Sets the maximum movement rate a creature can have while walking (not running).
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="fWalkRate">The walk rate cap. Setting to -1.0 removes the cap. Default is 2000.0 (base human walk speed).</param>
        /// <remarks>
        /// This sets a cap on the creature's walking speed, allowing creatures with movement speed enhancements to walk at a normal rate.
        /// This is useful for maintaining realistic movement speeds while allowing for enhanced running speeds.
        /// Setting the value to -1.0 removes the cap entirely.
        /// </remarks>
        public void SetWalkRateCap(uint creature, float fWalkRate)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetWalkRateCap(creature, fWalkRate);
        }

        /// <inheritdoc/>
        public void SetGold(uint creature, int gold)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetGold(creature, gold);
        }

        /// <inheritdoc/>
        public void SetCorpseDecayTime(uint creature, int decayTimeMs)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetCorpseDecayTime(creature, decayTimeMs);
        }

        /// <summary>
        /// Retrieves the creature's base saving throw value and any modifiers set in the toolset.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="which">The saving throw type to query. Use SavingThrow enum values.</param>
        /// <returns>The base saving throw value for the specified type.</returns>
        /// <remarks>
        /// This returns the creature's base saving throw value before any ability score or other modifiers.
        /// The base saving throw is determined by the creature's class levels and is modified by ability scores.
        /// Use SetBaseSavingThrow() to modify this value.
        /// </remarks>
        public int GetBaseSavingThrow(uint creature, int which)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetBaseSavingThrow(creature, which);
        }

        /// <inheritdoc/>
        public void SetBaseSavingThrow(uint creature, SavingThrowCategoryType which, int value)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetBaseSavingThrow(creature, (int)which, value);
        }

        /// <inheritdoc/>
        public void LevelUp(uint creature, ClassType classId, int count = 1)
        {
            global::NWN.Core.NWNX.CreaturePlugin.LevelUp(creature, (int)classId, count);
        }

        /// <summary>
        /// Removes the specified number of levels from the creature, starting with the most recent levels.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="count">The number of levels to remove. Must be positive. Default is 1.</param>
        /// <remarks>
        /// This removes levels from the creature, starting with the most recently gained levels.
        /// This will not work on player characters - only NPCs and other creatures.
        /// The creature will lose all benefits of the removed levels including hit points, feats, and abilities.
        /// Use LevelUp() to add levels back if needed.
        /// </remarks>
        public void LevelDown(uint creature, int count = 1)
        {
            global::NWN.Core.NWNX.CreaturePlugin.LevelDown(creature, count);
        }

        /// <inheritdoc/>
        public void SetChallengeRating(uint creature, float fCR)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetChallengeRating(creature, fCR);
        }

        /// <inheritdoc/>
        public int GetAttackBonus(uint creature, bool isMelee = true, bool isTouchAttack = false,
            bool isOffhand = false, bool includeBaseAttackBonus = true)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetAttackBonus(creature, isMelee ? 1 : 0, isTouchAttack ? 1 : 0, isOffhand ? 1 : 0, includeBaseAttackBonus ? 1 : 0);
        }

        /// <summary>
        /// Retrieves the highest level version of a feat possessed by the creature.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="feat">The feat ID to check. Must be a valid feat ID.</param>
        /// <returns>The highest level of the feat possessed by the creature.</returns>
        /// <remarks>
        /// This returns the highest level version of a feat that the creature possesses.
        /// This is useful for feats like Barbarian Rage that have multiple levels.
        /// Returns 0 if the creature does not possess the feat.
        /// </remarks>
        public int GetHighestLevelOfFeat(uint creature, int feat)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetHighestLevelOfFeat(creature, feat);
        }

        /// <inheritdoc/>
        public int GetFeatRemainingUses(uint creature, FeatType feat)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetFeatRemainingUses(creature, (int)feat);
        }

        /// <inheritdoc/>
        public int GetFeatTotalUses(uint creature, FeatType feat)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetFeatTotalUses(creature, (int)feat);
        }

        /// <inheritdoc/>
        public void SetFeatRemainingUses(uint creature, FeatType feat, int uses)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetFeatRemainingUses(creature, (int)feat, uses);
        }

        /// <inheritdoc/>
        public int GetTotalEffectBonus(uint creature, BonusType bonusType = BonusType.Attack,
            uint target = OBJECT_INVALID, bool isElemental = false,
            bool isForceMax = false, int saveType = -1, int saveSpecificType = -1, NWNSkillType skill = NWNSkillType.Invalid,
            int abilityScore = -1, bool isOffhand = false)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetTotalEffectBonus(creature, (int)bonusType, target, isElemental ? 1 : 0, isForceMax ? 1 : 0, saveType, saveSpecificType, (int)skill, abilityScore, isOffhand ? 1 : 0);
        }

        /// <summary>
        /// Sets the original first or last name of the creature.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="name">The name to set. Cannot be null or empty.</param>
        /// <param name="isLastName">True to set the last name, false to set the first name.</param>
        /// <remarks>
        /// This sets the creature's original name, which is stored separately from their display name.
        /// For player characters, this will persist to the .bic file if saved.
        /// A relog is required to see the name change take effect.
        /// This is useful for tracking the creature's original identity.
        /// </remarks>
        public void SetOriginalName(uint creature, string name, bool isLastName)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetOriginalName(creature, name, isLastName ? 1 : 0);
        }

        /// <summary>
        /// Retrieves the original first or last name of the creature.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="isLastName">True to get the last name, false to get the first name.</param>
        /// <returns>The original name of the creature.</returns>
        /// <remarks>
        /// This returns the creature's original name, which is stored separately from their display name.
        /// This is useful for tracking the creature's original identity.
        /// Returns empty string if no original name is set.
        /// </remarks>
        public string GetOriginalName(uint creature, bool isLastName)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetOriginalName(creature, isLastName ? 1 : 0);
        }

        /// <inheritdoc/>
        public void SetSpellResistance(uint creature, int sr)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetSpellResistance(creature, sr);
        }

        /// <inheritdoc/>
        public void SetAnimalCompanionName(uint creature, string name)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetAnimalCompanionName(creature, name);
        }

        /// <summary>
        /// Sets the creature's familiar's name.
        /// </summary>
        /// <param name="creature">The master creature object. Must be a valid creature with a familiar.</param>
        /// <param name="name">The name to set for the familiar. Cannot be null or empty.</param>
        /// <remarks>
        /// This sets the name of the creature's familiar.
        /// For player characters, this will persist to the .bic file if saved.
        /// A relog is required to see the name change take effect.
        /// Only works if the creature has an active familiar.
        /// </remarks>
        public void SetFamiliarName(uint creature, string name)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetFamiliarName(creature, name);
        }

        /// <inheritdoc/>
        public bool GetDisarmable(uint creature)
        {
            var result = global::NWN.Core.NWNX.CreaturePlugin.GetDisarmable(creature);
            return result != 0;
        }

        /// <inheritdoc/>
        public void SetDisarmable(uint creature, bool isDisarmable)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetDisarmable(creature, isDisarmable ? 1 : 0);
        }

        /// <summary>
        /// Sets one of the creature's domains for a specific class.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="class">The class type from the ClassType enum.</param>
        /// <param name="index">The domain index (0 for first domain, 1 for second domain).</param>
        /// <param name="domain">The domain ID to set. Must be a valid domain from domains.2da.</param>
        /// <remarks>
        /// This sets a specific domain for the creature's class.
        /// Domains provide special abilities and spell access for certain classes.
        /// Only certain classes (like Cleric) can have domains.
        /// </remarks>
        public void SetDomain(uint creature, ClassType @class, int index, int domain)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetDomain(creature, (int)@class, index, domain);
        }

        /// <inheritdoc/>
        public void SetSpecialization(uint creature, ClassType @class, int school)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetSpecialization(creature, (int)@class, school);
        }

        /// <inheritdoc/>
        public void SetFaction(uint creature, int factionId)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetFaction(creature, factionId);
        }

        /// <summary>
        /// Retrieves the creature's current faction ID.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The faction ID as an integer, or -1 if the creature is invalid.</returns>
        /// <remarks>
        /// This returns the creature's current faction membership.
        /// Returns -1 if the creature is invalid or has no faction.
        /// Faction determines how the creature interacts with other creatures and objects.
        /// </remarks>
        public int GetFaction(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetFaction(creature);
        }

        /// <inheritdoc/>
        public bool GetFlatFooted(uint creature)
        {
            var result = global::NWN.Core.NWNX.CreaturePlugin.GetFlatFooted(creature);
            return result != 0;
        }

        /// <inheritdoc/>
        public string SerializeQuickbar(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.SerializeQuickbar(creature);
        }

        /// <inheritdoc/>
        public bool DeserializeQuickbar(uint creature, string serializedQuickbar)
        {
            var result = global::NWN.Core.NWNX.CreaturePlugin.DeserializeQuickbar(creature, serializedQuickbar);
            return result != 0;
        }


        /// <inheritdoc/>
        public void SetEncounter(uint creature, uint encounter)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetEncounter(creature, encounter);
        }

        /// <inheritdoc/>
        public uint GetEncounter(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetEncounter(creature);
        }


        /// <summary>
        /// Overrides the damage level display of the creature.
        /// </summary>
        /// <param name="creature">The target creature object to modify. Must be a valid creature.</param>
        /// <param name="damageLevel">A damage level from damagelevels.2da (0-255), or -1 to remove the override.</param>
        /// <remarks>
        /// This overrides the damage level text displayed under the creature's name.
        /// Examples include "Near Death", "Badly Wounded", "Injured", etc.
        /// Use -1 to remove the override and return to normal damage level calculation.
        /// The damage level affects the visual appearance but not actual hit points.
        /// </remarks>
        public void OverrideDamageLevel(uint creature, int damageLevel)
        {
            global::NWN.Core.NWNX.CreaturePlugin.OverrideDamageLevel(creature, damageLevel);
        }

        /// <summary>
        /// Retrieves whether the creature is currently bartering.
        /// </summary>
        /// <param name="creature">The target creature object to query. Must be a valid creature.</param>
        /// <returns>True if the creature is currently bartering, false otherwise.</returns>
        /// <remarks>
        /// This indicates whether the creature is currently engaged in a barter/trade session.
        /// Returns false if the creature is not bartering or if there's an error.
        /// </remarks>
        public bool GetIsBartering(uint creature)
        {
            var result = global::NWN.Core.NWNX.CreaturePlugin.GetIsBartering(creature);
            return result != 0;
        }


        /// <inheritdoc/>
        public void SetLastItemCasterLevel(uint creature, int casterLevel)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetLastItemCasterLevel(creature, casterLevel);
        }

        /// <inheritdoc/>
        public int GetLastItemCasterLevel(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetLastItemCasterLevel(creature);
        }

        /// <summary>
        /// Retrieves the armor class of the attacked creature against the attacking creature.
        /// </summary>
        /// <param name="attacked">The creature being attacked. Must be a valid creature.</param>
        /// <param name="versus">The creature doing the attacking. Must be a valid creature.</param>
        /// <param name="touch">True for touch attacks, false for normal attacks. Default is false.</param>
        /// <returns>The armor class value, or -255 on error. Returns flat-footed AC if versus is invalid.</returns>
        /// <remarks>
        /// This calculates the effective armor class of the attacked creature against the specific attacker.
        /// Touch attacks ignore armor bonuses but not deflection bonuses.
        /// Returns -255 if there's an error in the calculation.
        /// </remarks>
        public int GetArmorClassVersus(uint attacked, uint versus, bool touch = false)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetArmorClassVersus(attacked, versus, touch ? 1 : 0);
        }

        /// <inheritdoc/>
        public void JumpToLimbo(uint creature)
        {
            global::NWN.Core.NWNX.CreaturePlugin.JumpToLimbo(creature);
        }

        /// <inheritdoc/>
        public void SetCriticalMultiplierModifier(uint creature, int modifier, int hand = 0, bool persist = false, BaseItemType baseItemType = BaseItemType.Invalid)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetCriticalMultiplierModifier(creature, modifier, hand, persist ? 1 : 0, (int)baseItemType);
        }

        /// <inheritdoc/>
        public int GetCriticalMultiplierModifier(uint creature, int hand = 0, BaseItemType baseItemType = BaseItemType.Invalid)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetCriticalMultiplierModifier(creature, hand, (int)baseItemType);
        }

        /// <inheritdoc/>
        public void SetCriticalMultiplierOverride(uint creature, int @override, int hand = 0, bool persist = false, BaseItemType baseItemType = BaseItemType.Invalid)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetCriticalMultiplierOverride(creature, @override, hand, persist ? 1 : 0, (int)baseItemType);
        }

        /// <inheritdoc/>
        public int GetCriticalMultiplierOverride(uint creature, int hand = 0, BaseItemType baseItemType = BaseItemType.Invalid)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetCriticalMultiplierOverride(creature, hand, (int)baseItemType);
        }

        /// <inheritdoc/>
        public void SetCriticalRangeModifier(uint creature, int modifier, int hand = 0, bool persist = false, BaseItemType baseItemType = BaseItemType.Invalid)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetCriticalRangeModifier(creature, modifier, hand, persist ? 1 : 0, (int)baseItemType);
        }

        /// <inheritdoc/>
        public int GetCriticalRangeModifier(uint creature, int hand = 0, BaseItemType baseItemType = BaseItemType.Invalid)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetCriticalRangeModifier(creature, hand, (int)baseItemType);
        }

        /// <inheritdoc/>
        public void SetCriticalRangeOverride(uint creature, int @override, int hand = 0, bool persist = false, BaseItemType baseItemType = BaseItemType.Invalid)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetCriticalRangeOverride(creature, @override, hand, persist ? 1 : 0, (int)baseItemType);
        }

        /// <summary>
        /// Retrieves the critical range override for the creature.
        /// </summary>
        /// <param name="creature">The target creature object to query. Must be a valid creature.</param>
        /// <param name="hand">The hand to query: 0 for all attacks, 1 for Mainhand, 2 for Offhand. Default is 0.</param>
        /// <param name="baseItemType">The base item type to query. Use BaseItem.Invalid for all types. Default is Invalid.</param>
        /// <returns>The current critical range override for the creature.</returns>
        /// <remarks>
        /// This returns the current critical range override for the specified hand and item type.
        /// Returns 0 if no override is set or if there's an error.
        /// </remarks>
        public int GetCriticalRangeOverride(uint creature, int hand = 0, BaseItemType baseItemType = BaseItemType.Invalid)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetCriticalRangeOverride(creature, hand, (int)baseItemType);
        }

        /// <summary>
        /// Adds an associate to the creature.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="associate">The associate object to add. Must be a valid creature.</param>
        /// <param name="associateType">The associate type. See associate types in the game constants.</param>
        /// <remarks>
        /// This adds an associate to the creature's party or group.
        /// Associates can be followers, summoned creatures, or other allied creatures.
        /// The associate type determines the relationship and behavior of the associate.
        /// </remarks>
        public void AddAssociate(uint creature, uint associate, int associateType)
        {
            global::NWN.Core.NWNX.CreaturePlugin.AddAssociate(creature, associate, associateType);
        }

        /// <inheritdoc/>
        public int GetWalkAnimation(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetWalkAnimation(creature);
        }

        /// <inheritdoc/>
        public void SetWalkAnimation(uint creature, int animation)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetWalkAnimation(creature, animation);
        }

        /// <summary>
        /// Sets the attack roll override for the creature.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="roll">The roll value to override with. Must be between 1 and 20.</param>
        /// <param name="modifier">The modifier value to apply. Can be positive or negative.</param>
        /// <remarks>
        /// This overrides the creature's attack roll with a specific value.
        /// The roll parameter should be between 1 and 20 (d20 roll).
        /// The modifier is added to the roll value for the final attack roll.
        /// This is useful for testing or special combat scenarios.
        /// </remarks>
        public void SetAttackRollOverride(uint creature, int roll, int modifier)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetAttackRollOverride(creature, roll, modifier);
        }

        /// <inheritdoc/>
        public void SetParryAllAttacks(uint creature, bool parry)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetParryAllAttacks(creature, parry ? 1 : 0);
        }

        /// <inheritdoc/>
        public bool GetNoPermanentDeath(uint creature)
        {
            var result = global::NWN.Core.NWNX.CreaturePlugin.GetNoPermanentDeath(creature);
            return result != 0;
        }

        /// <summary>
        /// Sets whether the creature has no permanent death.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="noPermanentDeath">True if the creature has no permanent death, false otherwise.</param>
        /// <remarks>
        /// This controls whether the creature is immune to permanent death.
        /// When enabled, the creature cannot be permanently killed and will respawn.
        /// This is typically used for important NPCs or special creatures.
        /// </remarks>
        public void SetNoPermanentDeath(uint creature, bool noPermanentDeath)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetNoPermanentDeath(creature, noPermanentDeath ? 1 : 0);
        }

        /// <inheritdoc/>
        public Vector3 ComputeSafeLocation(uint creature, Vector3 position, float radius = 20.0f, bool walkStraightLineRequired = true)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.ComputeSafeLocation(creature, position, radius, walkStraightLineRequired ? 1 : 0);
        }

        /// <inheritdoc/>
        public void DoPerceptionUpdateOnCreature(uint creature, uint targetCreature)
        {
            global::NWN.Core.NWNX.CreaturePlugin.DoPerceptionUpdateOnCreature(creature, targetCreature);
        }

        /// <summary>
        /// Retrieves the personal space value of the creature.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The personal space value in meters.</returns>
        /// <remarks>
        /// This returns the creature's personal space radius.
        /// Personal space determines how close other creatures can get before the creature reacts.
        /// Larger values mean the creature needs more space around it.
        /// </remarks>
        public float GetPersonalSpace(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetPersonalSpace(creature);
        }

        /// <inheritdoc/>
        public void SetPersonalSpace(uint creature, float personalSpace)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetPersonalSpace(creature, personalSpace);
        }

        /// <inheritdoc/>
        public float GetCreaturePersonalSpace(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetCreaturePersonalSpace(creature);
        }

        /// <inheritdoc/>
        public void SetCreaturePersonalSpace(uint creature, float creaturePersonalSpace)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetCreaturePersonalSpace(creature, creaturePersonalSpace);
        }

        /// <inheritdoc/>
        public float GetHeight(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetHeight(creature);
        }

        /// <summary>
        /// Sets the height of the creature.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="height">The height value to set in meters. Must be positive.</param>
        /// <remarks>
        /// This sets the creature's height, which affects collision detection and visual appearance.
        /// The height is used for determining if the creature can fit in certain spaces.
        /// </remarks>
        public void SetHeight(uint creature, float height)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetHeight(creature, height);
        }

        /// <inheritdoc/>
        public float GetHitDistance(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetHitDistance(creature);
        }

        /// <inheritdoc/>
        public void SetHitDistance(uint creature, float hitDistance)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetHitDistance(creature, hitDistance);
        }

        /// <inheritdoc/>
        public float GetPreferredAttackDistance(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetPreferredAttackDistance(creature);
        }

        /// <inheritdoc/>
        public void SetPreferredAttackDistance(uint creature, float preferredAttackDistance)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetPreferredAttackDistance(creature, preferredAttackDistance);
        }

        /// <inheritdoc/>
        public int GetArmorCheckPenalty(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetArmorCheckPenalty(creature);
        }

        /// <inheritdoc/>
        public int GetShieldCheckPenalty(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetShieldCheckPenalty(creature);
        }

        /// <summary>
        /// Sets the bypass effect immunity for the creature.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="immunityType">The immunity type to bypass. See immunity types in game constants.</param>
        /// <param name="chance">The chance percentage to bypass the immunity (0-100). Default is 100.</param>
        /// <param name="persist">Whether the setting should persist to the .bic file. Default is false.</param>
        /// <remarks>
        /// This allows the creature to bypass a specific type of effect immunity.
        /// The chance parameter determines the probability of bypassing the immunity.
        /// This is useful for special abilities or magical effects that can overcome immunities.
        /// </remarks>
        public void SetBypassEffectImmunity(uint creature, int immunityType, int chance = 100, bool persist = false)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetBypassEffectImmunity(creature, immunityType, chance, persist ? 1 : 0);
        }

        /// <inheritdoc/>
        public int GetBypassEffectImmunity(uint creature, int immunityType)        
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetBypassEffectImmunity(creature, immunityType);
        }

        /// <inheritdoc/>
        public int GetNumberOfBonusSpells(uint creature, int multiClass, int spellLevel)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetNumberOfBonusSpells(creature, multiClass, spellLevel);
        }

        /// <summary>
        /// Modifies the creature's number of bonus spells for a specific class and spell level.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="multiClass">The character class position, starting at 0. Must be between 0 and 2.</param>
        /// <param name="spellLevel">The spell level to modify, 0 to 9.</param>
        /// <param name="delta">The value to change the number of bonus spells by. Can be positive or negative.</param>
        /// <remarks>
        /// This modifies the number of bonus spells the creature has for the specified class and spell level.
        /// The delta value is added to the current number of bonus spells.
        /// Use positive values to increase bonus spells, negative values to decrease them.
        /// </remarks>
        public void ModifyNumberBonusSpells(uint creature, int multiClass, int spellLevel, int delta)
        {
            global::NWN.Core.NWNX.CreaturePlugin.ModifyNumberBonusSpells(creature, multiClass, spellLevel, delta);
        }

        /// <inheritdoc/>
        public void SetCasterLevelModifier(uint creature, ClassType classId, int modifier, bool persist = false)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetCasterLevelModifier(creature, (int)classId, modifier, persist ? 1 : 0);
        }

        /// <inheritdoc/>
        public int GetCasterLevelModifier(uint creature, ClassType classId)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetCasterLevelModifier(creature, (int)classId);
        }

        /// <summary>
        /// Sets a caster level override for the creature.
        /// </summary>
        /// <param name="creature">The target creature object to modify. Must be a valid creature.</param>
        /// <param name="classId">The class that this override will apply to. See ClassType enum for available classes.</param>
        /// <param name="casterLevel">The caster level override to apply. Must be positive.</param>
        /// <param name="persist">Whether the override should persist to the .bic file if applicable. Default is false.</param>
        /// <remarks>
        /// This sets a caster level override for the creature's specified class.
        /// Unlike modifiers, overrides completely replace the creature's base caster level for that class.
        /// This affects spell power, duration, and other caster level dependent effects.
        /// </remarks>
        public void SetCasterLevelOverride(uint creature, ClassType classId, int casterLevel, bool persist = false)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetCasterLevelOverride(creature, (int)classId, casterLevel, persist ? 1 : 0);
        }

        /// <summary>
        /// Gets the current caster level override for the creature.
        /// </summary>
        /// <param name="creature">The target creature object to query. Must be a valid creature.</param>
        /// <param name="classId">The creature caster class. See ClassType enum for available classes.</param>
        /// <returns>The current caster level override for the creature, or -1 if not set.</returns>
        /// <remarks>
        /// This returns the current caster level override for the creature's specified class.
        /// Returns -1 if no override is set or if there's an error.
        /// </remarks>
        public int GetCasterLevelOverride(uint creature, ClassType classId)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetCasterLevelOverride(creature, (int)classId);
        }
    }
}




