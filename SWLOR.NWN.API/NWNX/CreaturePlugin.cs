using System.Numerics;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.NWN.API.NWNX
{
    public static class CreaturePlugin
    {
        /// <summary>
        /// Gives the provided creature the provided feat.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="feat">The feat id.</param>
        /// <remarks>Consider also using AddFeatByLevel() to properly allocate the feat to a level</remarks>
        public static void AddFeat(uint creature, FeatType feat)
        {
            global::NWN.Core.NWNX.CreaturePlugin.AddFeat(creature, (int)feat);
        }

        /// <summary>
        /// Gives the creature a feat assigned at a level.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="feat">The feat id.</param>
        /// <param name="level">The level they gained the feat.</param>
        /// <remarks>Adds the feat to the stat list at the provided level.</remarks>
        public static void AddFeatByLevel(uint creature, FeatType feat, int level)
        {
            global::NWN.Core.NWNX.CreaturePlugin.AddFeatByLevel(creature, (int)feat, level);
        }

        /// <summary>
        /// Removes a feat from a creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="feat">The feat id.</param>
        public static void RemoveFeat(uint creature, FeatType feat)
        {
            global::NWN.Core.NWNX.CreaturePlugin.RemoveFeat(creature, (int)feat);
        }

        /// <summary>
        /// Determines if the creature knows a feat.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="feat">The feat id.</param>
        /// <returns>True if the creature has the feat, regardless if they have any usages left or not.</returns>
        /// <remarks>This differs from native GetHasFeat which returns false if the feat has no more uses per day.</remarks>
        public static bool GetKnowsFeat(uint creature, FeatType feat)
        {
            var result = global::NWN.Core.NWNX.CreaturePlugin.GetKnowsFeat(creature, (int)feat);
            return result != 0;
        }

        /// <summary>
        /// Returns the count of feats learned at the provided level.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="level">The level.</param>
        /// <returns>The count of feats.</returns>
        public static int GetFeatCountByLevel(uint creature, int level)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetFeatCountByLevel(creature, level);
        }

        /// <summary>
        /// Returns the feat learned at the level and index.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="level">The level.</param>
        /// <param name="index">The index. Index bounds: 0 <= index < GetFeatCountByLevel().</param>
        /// <returns>The feat id at the index.</returns>
        public static FeatType GetFeatByLevel(uint creature, int level, int index)
        {
            var result = global::NWN.Core.NWNX.CreaturePlugin.GetFeatByLevel(creature, level, index);
            return (FeatType)result;
        }

        /// <summary>
        /// Get the total number of feats known by creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <returns>The total feat count for the creature.</returns>
        public static int GetFeatCount(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetFeatCount(creature);
        }

        /// <summary>
        /// Returns the creature's feat at a given index.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="index">The index. Index bounds: 0 <= index < GetFeatCount().</param>
        /// <returns>The feat id at the index.</returns>
        public static FeatType GetFeatByIndex(uint creature, int index)
        {
            var result = global::NWN.Core.NWNX.CreaturePlugin.GetFeatByIndex(creature, index);
            return (FeatType)result;
        }

        /// <summary>
        /// Gets if creature meets feat requirements.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="feat">The feat id.</param>
        /// <returns>True if creature meets all requirements to take given feat</returns>
        public static bool GetMeetsFeatRequirements(uint creature, FeatType feat)
        {
            var result = global::NWN.Core.NWNX.CreaturePlugin.GetMeetsFeatRequirements(creature, (int)feat);
            return result != 0;
        }

        /// <summary>
        /// Returns the creature's special ability at a given index.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="index">The index. Index bounds: 0 <= index < GetSpecialAbilityCount().</param>
        /// <returns>An SpecialAbilitySlot struct.</returns>
        public static SpecialAbilitySlot GetSpecialAbility(uint creature, int index)
        {
            var coreAbility = global::NWN.Core.NWNX.CreaturePlugin.GetSpecialAbility(creature, index);
            return new SpecialAbilitySlot
            {
                Level = coreAbility.level,
                Ready = coreAbility.ready,
                ID = coreAbility.id
            };
        }

        /// <summary>
        /// Gets the count of special abilities of the creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <returns>The total special ability count.</returns>
        public static int GetSpecialAbilityCount(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetSpecialAbilityCount(creature);
        }

        /// <summary>
        /// Adds a special ability to a creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="ability">An SpecialAbilitySlot struct.</param>
        public static void AddSpecialAbility(uint creature, SpecialAbilitySlot ability)
        {
            var coreAbility = new global::NWN.Core.NWNX.SpecialAbility
            {
                id = ability.ID,
                ready = ability.Ready,
                level = ability.Level
            };
            global::NWN.Core.NWNX.CreaturePlugin.AddSpecialAbility(creature, coreAbility);
        }

        /// <summary>
        /// Removes a special ability from a creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="index">The index. Index bounds: 0 <= index < GetSpecialAbilityCount().</param>
        public static void RemoveSpecialAbility(uint creature, int index)
        {
            global::NWN.Core.NWNX.CreaturePlugin.RemoveSpecialAbility(creature, index);
        }

        /// <summary>
        /// Sets a special ability at the index for the creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="index">The index. Index bounds: 0 <= index < GetSpecialAbilityCount().</param>
        /// <param name="ability">An SpecialAbilitySlot struct.</param>
        public static void SetSpecialAbility(uint creature, int index, SpecialAbilitySlot ability)
        {
            var coreAbility = new global::NWN.Core.NWNX.SpecialAbility
            {
                id = ability.ID,
                ready = ability.Ready,
                level = ability.Level
            };
            global::NWN.Core.NWNX.CreaturePlugin.SetSpecialAbility(creature, index, coreAbility);
        }

        /// <summary>
        /// Get the class taken by the creature at the provided level.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="level">The level.</param>
        /// <returns>The class id.</returns>
        public static ClassType GetClassByLevel(uint creature, int level)
        {
            var result = global::NWN.Core.NWNX.CreaturePlugin.GetClassByLevel(creature, level);
            return (ClassType)result;
        }

        /// <summary>
        /// Sets the base AC for the creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="ac">The base AC to set for the creature.</param>
        public static void SetBaseAC(uint creature, int ac)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetBaseAC(creature, ac);
        }

        /// <summary>
        /// Get the base AC for the creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <returns>The base AC.</returns>
        public static int GetBaseAC(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetBaseAC(creature);
        }

        /// <summary>
        /// Sets the provided ability score of provided creature to the provided value.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="ability">The ability type.</param>
        /// <param name="value">The value to set.</param>
        /// <remarks>Does not apply racial bonuses/penalties.</remarks>
        public static void SetRawAbilityScore(uint creature, AbilityType ability, int value)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetRawAbilityScore(creature, (int)ability, value);
        }

        /// <summary>
        /// Gets the provided ability score of provided creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="ability">The ability type.</param>
        /// <returns>The raw ability score.</returns>
        /// <remarks>Does not apply racial bonuses/penalties.</remarks>
        public static int GetRawAbilityScore(uint creature, AbilityType ability)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetRawAbilityScore(creature, (int)ability);
        }

        /// <summary>
        /// Adjusts the provided ability score of a provided creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="ability">The ability type.</param>
        /// <param name="modifier">The modifier to apply.</param>
        /// <remarks>Does not apply racial bonuses/penalties.</remarks>
        public static void ModifyRawAbilityScore(uint creature, AbilityType ability, int modifier)
        {
            global::NWN.Core.NWNX.CreaturePlugin.ModifyRawAbilityScore(creature, (int)ability, modifier);
        }

        /// <summary>
        /// Gets the raw ability score a polymorphed creature had prior to polymorphing.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="ability">The ability type.</param>
        /// <returns>The pre-polymorph ability score.</returns>
        /// <remarks>Str/Dex/Con only.</remarks>
        public static int GetPrePolymorphAbilityScore(uint creature, AbilityType ability)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetPrePolymorphAbilityScore(creature, (int)ability);
        }

        /// <summary>
        /// Gets the remaining spell slots (innate casting) for the provided creature for the provided classId and level.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="classId">The class id.</param>
        /// <param name="level">The spell level.</param>
        /// <returns>The remaining spell slots.</returns>
        public static int GetRemainingSpellSlots(uint creature, ClassType classId, int level)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetRemainingSpellSlots(creature, (int)classId, level);
        }

        /// <summary>
        /// Sets the remaining spell slots (innate casting) for the provided creature for the provided classId and level.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="classId">The class id.</param>
        /// <param name="level">The spell level.</param>
        /// <param name="slots">The number of slots to set.</param>
        public static void SetRemainingSpellSlots(uint creature, ClassType classId, int level, int slots)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetRemainingSpellSlots(creature, (int)classId, level, slots);
        }

        /// <summary>
        /// Remove a spell from creature's spellbook for class.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="classId">The class id.</param>
        /// <param name="level">The spell level.</param>
        /// <param name="spellId">The spell id to remove.</param>
        public static void RemoveKnownSpell(uint creature, ClassType classId, int level, int spellId)
        {
            global::NWN.Core.NWNX.CreaturePlugin.RemoveKnownSpell(creature, (int)classId, level, spellId);
        }

        /// <summary>
        /// Add a new spell to creature's spellbook for class.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="classId">The class id.</param>
        /// <param name="level">The spell level.</param>
        /// <param name="spellId">The spell id to add.</param>
        public static void AddKnownSpell(uint creature, ClassType classId, int level, int spellId)
        {
            global::NWN.Core.NWNX.CreaturePlugin.AddKnownSpell(creature, (int)classId, level, spellId);
        }

        /// <summary>
        /// Gets the maximum count of spell slots for the provided creature for the provided classId and level.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="classId">The class id.</param>
        /// <param name="level">The spell level.</param>
        /// <returns>The maximum spell slots.</returns>
        public static int GetMaxSpellSlots(uint creature, ClassType classId, int level)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetMaxSpellSlots(creature, (int)classId, level);
        }

        /// <summary>
        /// Gets the maximum hit points for creature for level.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="level">The level.</param>
        /// <returns>The maximum hit points for that level.</returns>
        public static int GetMaxHitPointsByLevel(uint creature, int level)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetMaxHitPointsByLevel(creature, level);
        }

        /// <summary>
        /// Sets the maximum hit points for creature for level to nValue.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="level">The level.</param>
        /// <param name="value">The value to set.</param>
        public static void SetMaxHitPointsByLevel(uint creature, int level, int value)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetMaxHitPointsByLevel(creature, level, value);
        }

        /// <summary>
        /// Set creature's movement rate.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="rate">The movement rate.</param>
        public static void SetMovementRate(uint creature, MovementRate rate)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetMovementRate(creature, (int)rate);
        }

        /// <summary>
        /// Returns the creature's current movement rate factor (base = 1.0).
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <returns>The movement rate factor.</returns>
        public static float GetMovementRateFactor(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetMovementRateFactor(creature);
        }

        /// <summary>
        /// Sets the creature's current movement rate factor (base = 1.0).
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="factor">The movement rate factor.</param>
        public static void SetMovementRateFactor(uint creature, float factor)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetMovementRateFactor(creature, factor);
        }

        /// <summary>
        /// Set creature's raw good/evil alignment value.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="value">The alignment value.</param>
        public static void SetAlignmentGoodEvil(uint creature, int value)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetAlignmentGoodEvil(creature, value);
        }

        /// <summary>
        /// Set creature's raw law/chaos alignment value.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="value">The alignment value.</param>
        public static void SetAlignmentLawChaos(uint creature, int value)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetAlignmentLawChaos(creature, value);
        }

        /// <summary>
        /// Set the base ranks in a skill for creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="skill">The skill type.</param>
        /// <param name="rank">The skill rank.</param>
        public static void SetSkillRank(uint creature, NWNSkillType skill, int rank)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetSkillRank(creature, (int)skill, rank);
        }

        /// <summary>
        /// Set the classId ID in a particular position for a creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="position">Position should be 0, 1, or 2.</param>
        /// <param name="classId">ClassID should be a valid ID number in classes.2da and be between 0 and 255.</param>
        /// <param name="updateLevels">Whether to update levels.</param>
        public static void SetClassByPosition(uint creature, int position, ClassType classId, bool updateLevels = true)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetClassByPosition(creature, position, (int)classId, updateLevels ? 1 : 0);
        }

        /// <summary>
        /// Set creature's base attack bonus (BAB).
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="bab">The BAB value should be between 0 and 254.</param>
        /// <remarks>
        /// Modifying the BAB will also affect the creature's attacks per round and its
        /// eligibility for feats, prestige classes, etc.
        /// Setting BAB to 0 will cause the creature to revert to its original BAB based
        /// on its classes and levels. A creature can never have an actual BAB of zero.
        /// NOTE: The base game has a function SetBaseAttackBonus(), which actually sets
        /// the bonus attacks per round for a creature, not the BAB.
        /// </remarks>
        public static void SetBaseAttackBonus(uint creature, int bab)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetBaseAttackBonus(creature, bab);
        }

        /// <summary>
        /// Gets the creatures current attacks per round (using equipped weapon).
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="bBaseAPR">If true, will return the base attacks per round, based on BAB and equipped weapons, regardless of overrides set by calls to SetBaseAttackBonus() builtin function.</param>
        /// <returns>The number of attacks per round.</returns>
        public static int GetAttacksPerRound(uint creature, bool bBaseAPR)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetAttacksPerRound(creature, bBaseAPR ? 1 : 0);
        }

        /// <summary>
        /// Restore all creature feat uses.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        public static void RestoreFeats(uint creature)
        {
            global::NWN.Core.NWNX.CreaturePlugin.RestoreFeats(creature);
        }

        /// <summary>
        /// Restore all creature special ability uses.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        public static void RestoreSpecialAbilities(uint creature)
        {
            global::NWN.Core.NWNX.CreaturePlugin.RestoreSpecialAbilities(creature);
        }

        /// <summary>
        /// Restore uses for all items carried by the creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        public static void RestoreItems(uint creature)
        {
            global::NWN.Core.NWNX.CreaturePlugin.RestoreItems(creature);
        }

        /// <summary>
        /// Sets the creature size. Use CREATURE_SIZE_* constants.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="creatureSize">The creature size.</param>
        public static void SetSize(uint creature, CreatureSize creatureSize)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetSize(creature, (int)creatureSize);
        }

        /// <summary>
        /// Gets the creature's remaining unspent skill points.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <returns>The remaining skill points.</returns>
        public static int GetSkillPointsRemaining(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetSkillPointsRemaining(creature);
        }


        /// <summary>
        /// Sets the creature's remaining unspent skill points.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="skillpoints">The skill points to set.</param>
        public static void SetSkillPointsRemaining(uint creature, int skillpoints)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetSkillPointsRemaining(creature, skillpoints);
        }

        /// <summary>
        /// Sets the creature's racial type.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="racialtype">The racial type.</param>
        public static void SetRacialType(uint creature, RacialType racialtype)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetRacialType(creature, (int)racialtype);
        }

        /// <summary>
        /// Returns the creature's current movement type (MOVEMENT_TYPE_*).
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <returns>The movement type.</returns>
        public static MovementType GetMovementType(uint creature)
        {
            var result = global::NWN.Core.NWNX.CreaturePlugin.GetMovementType(creature);
            return (MovementType)result;
        }

        /// <summary>
        /// Sets the maximum movement rate a creature can have while walking (not running).
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="fWalkRate">The walk rate cap. Setting the value to -1.0 will remove the cap. Default value is 2000.0, which is the base human walk speed.</param>
        /// <remarks>This allows a creature with movement speed enhancements to walk at a normal rate.</remarks>
        public static void SetWalkRateCap(uint creature, float fWalkRate)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetWalkRateCap(creature, fWalkRate);
        }

        /// <summary>
        /// Sets the creature's gold without sending a feedback message.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="gold">The gold amount.</param>
        public static void SetGold(uint creature, int gold)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetGold(creature, gold);
        }

        /// <summary>
        /// Sets corpse decay time in milliseconds.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="decayTimeMs">The decay time in milliseconds.</param>
        public static void SetCorpseDecayTime(uint creature, int decayTimeMs)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetCorpseDecayTime(creature, decayTimeMs);
        }

        /// <summary>
        /// Returns the creature's base save and any modifiers set in the toolset.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="which">The saving throw type.</param>
        /// <returns>The base saving throw value.</returns>
        public static int GetBaseSavingThrow(uint creature, int which)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetBaseSavingThrow(creature, which);
        }

        /// <summary>
        /// Sets the base saving throw of the creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="which">The saving throw type.</param>
        /// <param name="value">The value to set.</param>
        public static void SetBaseSavingThrow(uint creature, SavingThrow which, int value)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetBaseSavingThrow(creature, (int)which, value);
        }

        /// <summary>
        /// Add count levels of class to the creature, bypassing all validation.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="classId">The class id.</param>
        /// <param name="count">The number of levels to add.</param>
        /// <remarks>This will not work on player characters.</remarks>
        public static void LevelUp(uint creature, ClassType classId, int count = 1)
        {
            global::NWN.Core.NWNX.CreaturePlugin.LevelUp(creature, (int)classId, count);
        }

        /// <summary>
        /// Remove last count levels from a creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="count">The number of levels to remove.</param>
        /// <remarks>This will not work on player characters.</remarks>
        public static void LevelDown(uint creature, int count = 1)
        {
            global::NWN.Core.NWNX.CreaturePlugin.LevelDown(creature, count);
        }

        /// <summary>
        /// Sets the creature's challenge rating.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="fCR">The challenge rating.</param>
        public static void SetChallengeRating(uint creature, float fCR)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetChallengeRating(creature, fCR);
        }

        /// <summary>
        /// Returns the creature's highest attack bonus based on its own stats.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="isMelee">True: Get Melee/Unarmed Attack Bonus, False: Get Ranged Attack Bonus.</param>
        /// <param name="isTouchAttack">Whether this is a touch attack.</param>
        /// <param name="isOffhand">Whether this is an offhand attack.</param>
        /// <param name="includeBaseAttackBonus">Whether to include base attack bonus.</param>
        /// <returns>The attack bonus.</returns>
        /// <remarks>NOTE: AB vs. &lt;Type&gt; and +AB on Gauntlets are excluded.</remarks>
        public static int GetAttackBonus(uint creature, bool isMelee = true, bool isTouchAttack = false,
            bool isOffhand = false, bool includeBaseAttackBonus = true)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetAttackBonus(creature, isMelee ? 1 : 0, isTouchAttack ? 1 : 0, isOffhand ? 1 : 0, includeBaseAttackBonus ? 1 : 0);
        }

        /// <summary>
        /// Get highest level version of feat possessed by creature (e.g. for barbarian rage).
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="feat">The feat id.</param>
        /// <returns>The highest level of the feat.</returns>
        public static int GetHighestLevelOfFeat(uint creature, int feat)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetHighestLevelOfFeat(creature, feat);
        }

        /// <summary>
        /// Get feat remaining uses of a creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="feat">The feat id.</param>
        /// <returns>The remaining uses.</returns>
        public static int GetFeatRemainingUses(uint creature, FeatType feat)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetFeatRemainingUses(creature, (int)feat);
        }

        /// <summary>
        /// Get feat total uses of a creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="feat">The feat id.</param>
        /// <returns>The total uses.</returns>
        public static int GetFeatTotalUses(uint creature, FeatType feat)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetFeatTotalUses(creature, (int)feat);
        }

        /// <summary>
        /// Set feat remaining uses of a creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="feat">The feat id.</param>
        /// <param name="uses">The number of uses to set.</param>
        public static void SetFeatRemainingUses(uint creature, FeatType feat, int uses)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetFeatRemainingUses(creature, (int)feat, uses);
        }

        /// <summary>
        /// Get total effect bonus.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="bonusType">The bonus type.</param>
        /// <param name="target">The target object.</param>
        /// <param name="isElemental">Whether this is elemental.</param>
        /// <param name="isForceMax">Whether to force maximum.</param>
        /// <param name="saveType">The save type.</param>
        /// <param name="saveSpecificType">The specific save type.</param>
        /// <param name="skill">The skill type.</param>
        /// <param name="abilityScore">The ability score.</param>
        /// <param name="isOffhand">Whether this is offhand.</param>
        /// <returns>The total effect bonus.</returns>
        public static int GetTotalEffectBonus(uint creature, BonusType bonusType = BonusType.Attack,
            uint target = OBJECT_INVALID, bool isElemental = false,
            bool isForceMax = false, int saveType = -1, int saveSpecificType = -1, NWNSkillType skill = NWNSkillType.Invalid,
            int abilityScore = -1, bool isOffhand = false)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetTotalEffectBonus(creature, (int)bonusType, target, isElemental ? 1 : 0, isForceMax ? 1 : 0, saveType, saveSpecificType, (int)skill, abilityScore, isOffhand ? 1 : 0);
        }

        /// <summary>
        /// Set the original first or last name of creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="name">The name to set.</param>
        /// <param name="isLastName">Whether this is the last name.</param>
        /// <remarks>For PCs this will persist to the .bic file if saved. Requires a relog to update.</remarks>
        public static void SetOriginalName(uint creature, string name, bool isLastName)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetOriginalName(creature, name, isLastName ? 1 : 0);
        }

        /// <summary>
        /// Get the original first or last name of creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="isLastName">Whether this is the last name.</param>
        /// <returns>The original name.</returns>
        public static string GetOriginalName(uint creature, bool isLastName)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetOriginalName(creature, isLastName ? 1 : 0);
        }

        /// <summary>
        /// Set creature's spell resistance.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="sr">The spell resistance value.</param>
        public static void SetSpellResistance(uint creature, int sr)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetSpellResistance(creature, sr);
        }

        /// <summary>
        /// Set creature's animal companion's name.
        /// </summary>
        /// <param name="creature">The master creature object.</param>
        /// <param name="name">The name to give their animal companion.</param>
        public static void SetAnimalCompanionName(uint creature, string name)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetAnimalCompanionName(creature, name);
        }

        /// <summary>
        /// Set creature's familiar's name.
        /// </summary>
        /// <param name="creature">The master creature object.</param>
        /// <param name="name">The name to give their familiar.</param>
        public static void SetFamiliarName(uint creature, string name)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetFamiliarName(creature, name);
        }

        /// <summary>
        /// Get whether the creature can be disarmed.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <returns>True if the creature can be disarmed.</returns>
        public static bool GetDisarmable(uint creature)
        {
            var result = global::NWN.Core.NWNX.CreaturePlugin.GetDisarmable(creature);
            return result != 0;
        }

        /// <summary>
        /// Set whether a creature can be disarmed.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="isDisarmable">Set to true if the creature can be disarmed.</param>
        public static void SetDisarmable(uint creature, bool isDisarmable)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetDisarmable(creature, isDisarmable ? 1 : 0);
        }

        /// <summary>
        /// Sets one of creature's domains.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="class">The class id from classes.2da. (Not class index 0-2)</param>
        /// <param name="index">The first or second domain.</param>
        /// <param name="domain">The domain constant to set.</param>
        public static void SetDomain(uint creature, ClassType @class, int index, int domain)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetDomain(creature, (int)@class, index, domain);
        }

        /// <summary>
        /// Sets creature's specialist school.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="class">The class id from classes.2da. (Not class index 0-2)</param>
        /// <param name="school">The school constant.</param>
        public static void SetSpecialization(uint creature, ClassType @class, int school)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetSpecialization(creature, (int)@class, school);
        }

        /// <summary>
        /// Sets creature's faction to be the faction with id factionId.
        /// </summary>
        /// <param name="creature">The creature.</param>
        /// <param name="factionId">The faction id we want the creature to join.</param>
        public static void SetFaction(uint creature, int factionId)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetFaction(creature, factionId);
        }

        /// <summary>
        /// Gets the faction id from creature.
        /// </summary>
        /// <param name="creature">The creature we wish to query against.</param>
        /// <returns>Faction id as an integer, -1 when used against invalid creature or invalid object.</returns>
        public static int GetFaction(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetFaction(creature);
        }

        /// <summary>
        /// Get whether a creature is flat-footed.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <returns>True if the creature is flat-footed.</returns>
        public static bool GetFlatFooted(uint creature)
        {
            var result = global::NWN.Core.NWNX.CreaturePlugin.GetFlatFooted(creature);
            return result != 0;
        }

        /// <summary>
        /// Serialize creature's quickbar to a base64 string.
        /// </summary>
        /// <param name="creature">The creature.</param>
        /// <returns>A base64 string representation of creature's quickbar.</returns>
        public static string SerializeQuickbar(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.SerializeQuickbar(creature);
        }

        /// <summary>
        /// Deserialize serializedQuickbar for creature.
        /// </summary>
        /// <param name="creature">The creature.</param>
        /// <param name="serializedQuickbar">A base64 string of a quickbar.</param>
        /// <returns>True on success.</returns>
        public static bool DeserializeQuickbar(uint creature, string serializedQuickbar)
        {
            var result = global::NWN.Core.NWNX.CreaturePlugin.DeserializeQuickbar(creature, serializedQuickbar);
            return result != 0;
        }


        /// <summary>
        /// Set the encounter source of creature.
        /// </summary>
        /// <param name="creature">The target creature.</param>
        /// <param name="encounter">The source encounter.</param>
        public static void SetEncounter(uint creature, uint encounter)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetEncounter(creature, encounter);
        }

        /// <summary>
        /// Get the encounter source of creature.
        /// </summary>
        /// <param name="creature">The target creature.</param>
        /// <returns>The encounter, OBJECT_INVALID if not part of an encounter or on error.</returns>
        public static uint GetEncounter(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetEncounter(creature);
        }


        /// <summary>
        /// Override the damage level of creature.
        /// </summary>
        /// <param name="creature">The target creature.</param>
        /// <param name="damageLevel">A damage level, see damagelevels.2da. Allowed values: 0-255 or -1 to remove the override.</param>
        /// <remarks>Damage levels are the damage state under a creature's name, for example: 'Near Death'.</remarks>
        public static void OverrideDamageLevel(uint creature, int damageLevel)
        {
            global::NWN.Core.NWNX.CreaturePlugin.OverrideDamageLevel(creature, damageLevel);
        }


        /// <summary>
        /// Get if creature is currently bartering.
        /// </summary>
        /// <param name="creature">The target creature.</param>
        /// <returns>True if creature is bartering, false if not or on error.</returns>
        public static bool GetIsBartering(uint creature)
        {
            var result = global::NWN.Core.NWNX.CreaturePlugin.GetIsBartering(creature);
            return result != 0;
        }


        /// <summary>
        /// Sets caster level for the last item used. Use in a spellhook or spell event before to set caster level for any spells cast from the item.
        /// </summary>
        /// <param name="creature">The creature who used the item.</param>
        /// <param name="casterLevel">The desired caster level.</param>
        public static void SetLastItemCasterLevel(uint creature, int casterLevel)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetLastItemCasterLevel(creature, casterLevel);
        }

        /// <summary>
        /// Gets the caster level of the last item used.
        /// </summary>
        /// <param name="creature">The creature who used the item.</param>
        /// <returns>Returns the creatures last used item's level.</returns>
        public static int GetLastItemCasterLevel(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetLastItemCasterLevel(creature);
        }

        /// <summary>
        /// Gets the Armor class of attacked against versus.
        /// </summary>
        /// <param name="attacked">The one being attacked.</param>
        /// <param name="versus">The one doing the attacking.</param>
        /// <param name="touch">True for touch attacks.</param>
        /// <returns>-255 on Error, Flat footed AC if versus is invalid or the Attacked AC versus versus.</returns>
        public static int GetArmorClassVersus(uint attacked, uint versus, bool touch = false)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetArmorClassVersus(attacked, versus, touch ? 1 : 0);
        }

        /// <summary>
        /// Move a creature to limbo.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        public static void JumpToLimbo(uint creature)
        {
            global::NWN.Core.NWNX.CreaturePlugin.JumpToLimbo(creature);
        }

        /// <summary>
        /// Sets the critical hit multiplier modifier for the creature.
        /// </summary>
        /// <param name="creature">The target creature.</param>
        /// <param name="modifier">The modifier to apply.</param>
        /// <param name="hand">0 for all attacks, 1 for Mainhand, 2 for Offhand.</param>
        /// <param name="persist">Whether the modifier should persist to .bic file if applicable.</param>
        /// <param name="baseItemType">The base item type.</param>
        /// <remarks>Persistence is activated each server reset by first use of either 'SetCriticalMultiplier*' functions. Recommended to trigger on a dummy target OnModuleLoad to enable persistence.</remarks>
        public static void SetCriticalMultiplierModifier(uint creature, int modifier, int hand = 0, bool persist = false, BaseItem baseItemType = BaseItem.Invalid)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetCriticalMultiplierModifier(creature, modifier, hand, persist ? 1 : 0, (int)baseItemType);
        }

        /// <summary>
        /// Gets the critical hit multiplier modifier for the Creature.
        /// </summary>
        /// <param name="creature">The target creature.</param>
        /// <param name="hand">0 for all attacks, 1 for Mainhand, 2 for Offhand.</param>
        /// <param name="baseItemType">The base item type.</param>
        /// <returns>The current critical hit multiplier modifier for the creature.</returns>
        public static int GetCriticalMultiplierModifier(uint creature, int hand = 0, BaseItem baseItemType = BaseItem.Invalid)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetCriticalMultiplierModifier(creature, hand, (int)baseItemType);
        }

        /// <summary>
        /// Sets the critical hit multiplier override for the creature.
        /// </summary>
        /// <param name="creature">The target creature.</param>
        /// <param name="override">The override value.</param>
        /// <param name="hand">0 for all attacks, 1 for Mainhand, 2 for Offhand.</param>
        /// <param name="persist">Whether the override should persist to .bic file if applicable.</param>
        /// <param name="baseItemType">The base item type.</param>
        public static void SetCriticalMultiplierOverride(uint creature, int @override, int hand = 0, bool persist = false, BaseItem baseItemType = BaseItem.Invalid)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetCriticalMultiplierOverride(creature, @override, hand, persist ? 1 : 0, (int)baseItemType);
        }

        /// <summary>
        /// Gets the critical hit multiplier override for the creature.
        /// </summary>
        /// <param name="creature">The target creature.</param>
        /// <param name="hand">0 for all attacks, 1 for Mainhand, 2 for Offhand.</param>
        /// <param name="baseItemType">The base item type.</param>
        /// <returns>The current critical hit multiplier override for the creature.</returns>
        public static int GetCriticalMultiplierOverride(uint creature, int hand = 0, BaseItem baseItemType = BaseItem.Invalid)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetCriticalMultiplierOverride(creature, hand, (int)baseItemType);
        }

        /// <summary>
        /// Sets the critical range modifier for the creature.
        /// </summary>
        /// <param name="creature">The target creature.</param>
        /// <param name="modifier">The modifier to apply.</param>
        /// <param name="hand">0 for all attacks, 1 for Mainhand, 2 for Offhand.</param>
        /// <param name="persist">Whether the modifier should persist to .bic file if applicable.</param>
        /// <param name="baseItemType">The base item type.</param>
        public static void SetCriticalRangeModifier(uint creature, int modifier, int hand = 0, bool persist = false, BaseItem baseItemType = BaseItem.Invalid)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetCriticalRangeModifier(creature, modifier, hand, persist ? 1 : 0, (int)baseItemType);
        }

        /// <summary>
        /// Gets the critical range modifier for the creature.
        /// </summary>
        /// <param name="creature">The target creature.</param>
        /// <param name="hand">0 for all attacks, 1 for Mainhand, 2 for Offhand.</param>
        /// <param name="baseItemType">The base item type.</param>
        /// <returns>The current critical range modifier for the creature.</returns>
        public static int GetCriticalRangeModifier(uint creature, int hand = 0, BaseItem baseItemType = BaseItem.Invalid)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetCriticalRangeModifier(creature, hand, (int)baseItemType);
        }

        /// <summary>
        /// Sets the critical range override for the creature.
        /// </summary>
        /// <param name="creature">The target creature.</param>
        /// <param name="override">The override value.</param>
        /// <param name="hand">0 for all attacks, 1 for Mainhand, 2 for Offhand.</param>
        /// <param name="persist">Whether the override should persist to .bic file if applicable.</param>
        /// <param name="baseItemType">The base item type.</param>
        public static void SetCriticalRangeOverride(uint creature, int @override, int hand = 0, bool persist = false, BaseItem baseItemType = BaseItem.Invalid)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetCriticalRangeOverride(creature, @override, hand, persist ? 1 : 0, (int)baseItemType);
        }

        /// <summary>
        /// Gets the critical range override for the creature.
        /// </summary>
        /// <param name="creature">The target creature.</param>
        /// <param name="hand">0 for all attacks, 1 for Mainhand, 2 for Offhand.</param>
        /// <param name="baseItemType">The base item type.</param>
        /// <returns>The current critical range override for the creature.</returns>
        public static int GetCriticalRangeOverride(uint creature, int hand = 0, BaseItem baseItemType = BaseItem.Invalid)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetCriticalRangeOverride(creature, hand, (int)baseItemType);
        }

        /// <summary>
        /// Adds an associate to the creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="associate">The associate object.</param>
        /// <param name="associateType">The associate type.</param>
        public static void AddAssociate(uint creature, uint associate, int associateType)
        {
            global::NWN.Core.NWNX.CreaturePlugin.AddAssociate(creature, associate, associateType);
        }

        /// <summary>
        /// Gets the walk animation of the creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <returns>The walk animation id.</returns>
        public static int GetWalkAnimation(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetWalkAnimation(creature);
        }

        /// <summary>
        /// Sets the walk animation of the creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="animation">The animation id.</param>
        public static void SetWalkAnimation(uint creature, int animation)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetWalkAnimation(creature, animation);
        }

        /// <summary>
        /// Sets the attack roll override for the creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="roll">The roll value.</param>
        /// <param name="modifier">The modifier value.</param>
        public static void SetAttackRollOverride(uint creature, int roll, int modifier)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetAttackRollOverride(creature, roll, modifier);
        }

        /// <summary>
        /// Sets whether the creature can parry all attacks.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="parry">Whether the creature can parry all attacks.</param>
        public static void SetParryAllAttacks(uint creature, bool parry)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetParryAllAttacks(creature, parry ? 1 : 0);
        }

        /// <summary>
        /// Gets whether the creature has no permanent death.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <returns>True if the creature has no permanent death.</returns>
        public static bool GetNoPermanentDeath(uint creature)
        {
            var result = global::NWN.Core.NWNX.CreaturePlugin.GetNoPermanentDeath(creature);
            return result != 0;
        }

        /// <summary>
        /// Sets whether the creature has no permanent death.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="noPermanentDeath">Whether the creature has no permanent death.</param>
        public static void SetNoPermanentDeath(uint creature, bool noPermanentDeath)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetNoPermanentDeath(creature, noPermanentDeath ? 1 : 0);
        }

        /// <summary>
        /// Computes a safe location for the creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="position">The position to check.</param>
        /// <param name="radius">The radius to search.</param>
        /// <param name="walkStraightLineRequired">Whether a walk straight line is required.</param>
        /// <returns>A safe location vector.</returns>
        public static Vector3 ComputeSafeLocation(uint creature, Vector3 position, float radius = 20.0f, bool walkStraightLineRequired = true)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.ComputeSafeLocation(creature, position, radius, walkStraightLineRequired ? 1 : 0);
        }

        /// <summary>
        /// Does a perception update on the creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="targetCreature">The target creature.</param>
        public static void DoPerceptionUpdateOnCreature(uint creature, uint targetCreature)
        {
            global::NWN.Core.NWNX.CreaturePlugin.DoPerceptionUpdateOnCreature(creature, targetCreature);
        }

        /// <summary>
        /// Gets the personal space of the creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <returns>The personal space value.</returns>
        public static float GetPersonalSpace(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetPersonalSpace(creature);
        }

        /// <summary>
        /// Sets the personal space of the creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="personalSpace">The personal space value.</param>
        public static void SetPersonalSpace(uint creature, float personalSpace)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetPersonalSpace(creature, personalSpace);
        }

        /// <summary>
        /// Gets the creature personal space.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <returns>The creature personal space value.</returns>
        public static float GetCreaturePersonalSpace(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetCreaturePersonalSpace(creature);
        }

        /// <summary>
        /// Sets the creature personal space.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="creaturePersonalSpace">The creature personal space value.</param>
        public static void SetCreaturePersonalSpace(uint creature, float creaturePersonalSpace)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetCreaturePersonalSpace(creature, creaturePersonalSpace);
        }

        /// <summary>
        /// Gets the height of the creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <returns>The height value.</returns>
        public static float GetHeight(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetHeight(creature);
        }

        /// <summary>
        /// Sets the height of the creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="height">The height value.</param>
        public static void SetHeight(uint creature, float height)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetHeight(creature, height);
        }

        /// <summary>
        /// Gets the hit distance of the creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <returns>The hit distance value.</returns>
        public static float GetHitDistance(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetHitDistance(creature);
        }

        /// <summary>
        /// Sets the hit distance of the creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="hitDistance">The hit distance value.</param>
        public static void SetHitDistance(uint creature, float hitDistance)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetHitDistance(creature, hitDistance);
        }

        /// <summary>
        /// Gets the preferred attack distance of the creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <returns>The preferred attack distance value.</returns>
        public static float GetPreferredAttackDistance(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetPreferredAttackDistance(creature);
        }

        /// <summary>
        /// Sets the preferred attack distance of the creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="preferredAttackDistance">The preferred attack distance value.</param>
        public static void SetPreferredAttackDistance(uint creature, float preferredAttackDistance)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetPreferredAttackDistance(creature, preferredAttackDistance);
        }

        /// <summary>
        /// Gets the armor check penalty of the creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <returns>The armor check penalty value.</returns>
        public static int GetArmorCheckPenalty(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetArmorCheckPenalty(creature);
        }

        /// <summary>
        /// Gets the shield check penalty of the creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <returns>The shield check penalty value.</returns>
        public static int GetShieldCheckPenalty(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetShieldCheckPenalty(creature);
        }

        /// <summary>
        /// Sets the bypass effect immunity for the creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="immunityType">The immunity type.</param>
        /// <param name="chance">The chance value.</param>
        /// <param name="persist">Whether the setting should persist.</param>
        public static void SetBypassEffectImmunity(uint creature, int immunityType, int chance = 100, bool persist = false)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetBypassEffectImmunity(creature, immunityType, chance, persist ? 1 : 0);
        }

        /// <summary>
        /// Gets the bypass effect immunity for the creature.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="immunityType">The immunity type.</param>
        /// <returns>The bypass effect immunity value.</returns>
        public static int GetBypassEffectImmunity(uint creature, int immunityType)        
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetBypassEffectImmunity(creature, immunityType);
        }

        /// <summary>
        /// Gets the creature's number of bonus spells.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="multiClass">The character class position, starting at 0.</param>
        /// <param name="spellLevel">The spell level, 0 to 9.</param>
        /// <returns>The number of bonus spells.</returns>
        public static int GetNumberOfBonusSpells(uint creature, int multiClass, int spellLevel)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetNumberOfBonusSpells(creature, multiClass, spellLevel);
        }

        /// <summary>
        /// Modifies the creature's number of bonus spells.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <param name="multiClass">The character class position, starting at 0.</param>
        /// <param name="spellLevel">The spell level, 0 to 9.</param>
        /// <param name="delta">The value to change the number of bonus spells by. Can be negative.</param>
        public static void ModifyNumberBonusSpells(uint creature, int multiClass, int spellLevel, int delta)
        {
            global::NWN.Core.NWNX.CreaturePlugin.ModifyNumberBonusSpells(creature, multiClass, spellLevel, delta);
        }
    }
}