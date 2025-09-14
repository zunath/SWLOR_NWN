using System.Numerics;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.NWN.API.NWNX
{
    /// <summary>
    /// Provides comprehensive creature management functionality including feat management, ability scores,
    /// spell casting, combat statistics, special abilities, and advanced creature properties.
    /// This plugin allows for detailed manipulation of creature characteristics and behaviors.
    /// </summary>
    public static class CreaturePlugin
    {
        /// <summary>
        /// Adds a feat to the specified creature's feat list.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="feat">The feat type to add. See FeatType enum for available feats.</param>
        /// <remarks>
        /// This adds the feat to the creature's overall feat list but does not assign it to a specific level.
        /// For proper level-based feat allocation, consider using AddFeatByLevel() instead.
        /// The feat will be immediately available to the creature.
        /// </remarks>
        public static void AddFeat(uint creature, FeatType feat)
        {
            global::NWN.Core.NWNX.CreaturePlugin.AddFeat(creature, (int)feat);
        }

        /// <summary>
        /// Adds a feat to the creature's feat list and assigns it to a specific level.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="feat">The feat type to add. See FeatType enum for available feats.</param>
        /// <param name="level">The level at which the creature gained this feat. Must be a positive integer.</param>
        /// <remarks>
        /// This properly allocates the feat to a specific level in the creature's progression.
        /// This is the recommended method for adding feats as it maintains proper level-based feat tracking.
        /// The feat will be immediately available to the creature.
        /// </remarks>
        public static void AddFeatByLevel(uint creature, FeatType feat, int level)
        {
            global::NWN.Core.NWNX.CreaturePlugin.AddFeatByLevel(creature, (int)feat, level);
        }

        /// <summary>
        /// Removes a feat from the creature's feat list.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="feat">The feat type to remove. See FeatType enum for available feats.</param>
        /// <remarks>
        /// This removes the feat from the creature's overall feat list.
        /// The creature will immediately lose access to the feat and its benefits.
        /// This affects all instances of the feat regardless of which level it was gained at.
        /// </remarks>
        public static void RemoveFeat(uint creature, FeatType feat)
        {
            global::NWN.Core.NWNX.CreaturePlugin.RemoveFeat(creature, (int)feat);
        }

        /// <summary>
        /// Determines whether the creature knows a specific feat.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="feat">The feat type to check. See FeatType enum for available feats.</param>
        /// <returns>True if the creature knows the feat, false otherwise.</returns>
        /// <remarks>
        /// This method checks if the creature has the feat in their feat list, regardless of remaining uses.
        /// This differs from the native GetHasFeat function, which returns false if the feat has no more uses per day.
        /// Use this method when you need to check feat knowledge rather than availability.
        /// </remarks>
        public static bool GetKnowsFeat(uint creature, FeatType feat)
        {
            var result = global::NWN.Core.NWNX.CreaturePlugin.GetKnowsFeat(creature, (int)feat);
            return result != 0;
        }

        /// <summary>
        /// Retrieves the number of feats learned by the creature at a specific level.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="level">The level to check. Must be a positive integer.</param>
        /// <returns>The number of feats learned at the specified level.</returns>
        /// <remarks>
        /// This returns the count of feats that were specifically gained at the given level.
        /// Useful for tracking feat progression and level-based feat allocation.
        /// Returns 0 if the creature has no feats at the specified level.
        /// </remarks>
        public static int GetFeatCountByLevel(uint creature, int level)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetFeatCountByLevel(creature, level);
        }

        /// <summary>
        /// Retrieves a specific feat learned by the creature at a given level and index.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="level">The level to check. Must be a positive integer.</param>
        /// <param name="index">The index of the feat at that level. Must be between 0 and GetFeatCountByLevel() - 1.</param>
        /// <returns>The feat type at the specified level and index.</returns>
        /// <remarks>
        /// This retrieves a specific feat from the creature's level-based feat list.
        /// The index must be within the valid range for the specified level.
        /// Use GetFeatCountByLevel() to determine the valid index range.
        /// </remarks>
        public static FeatType GetFeatByLevel(uint creature, int level, int index)
        {
            var result = global::NWN.Core.NWNX.CreaturePlugin.GetFeatByLevel(creature, level, index);
            return (FeatType)result;
        }

        /// <summary>
        /// Retrieves the total number of feats known by the creature.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The total number of feats in the creature's feat list.</returns>
        /// <remarks>
        /// This returns the total count of all feats the creature knows, regardless of level.
        /// This includes feats gained through level progression, racial bonuses, and other sources.
        /// Use GetFeatByIndex() to retrieve specific feats from this list.
        /// </remarks>
        public static int GetFeatCount(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetFeatCount(creature);
        }

        /// <summary>
        /// Retrieves a specific feat from the creature's overall feat list by index.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="index">The index of the feat in the creature's feat list. Must be between 0 and GetFeatCount() - 1.</param>
        /// <returns>The feat type at the specified index.</returns>
        /// <remarks>
        /// This retrieves a feat from the creature's complete feat list by position.
        /// The index must be within the valid range for the creature's total feat count.
        /// Use GetFeatCount() to determine the valid index range.
        /// </remarks>
        public static FeatType GetFeatByIndex(uint creature, int index)
        {
            var result = global::NWN.Core.NWNX.CreaturePlugin.GetFeatByIndex(creature, index);
            return (FeatType)result;
        }

        /// <summary>
        /// Determines whether the creature meets all requirements to take a specific feat.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="feat">The feat type to check requirements for. See FeatType enum for available feats.</param>
        /// <returns>True if the creature meets all requirements to take the feat, false otherwise.</returns>
        /// <remarks>
        /// This checks all prerequisites for the feat including ability scores, other feats, class levels, etc.
        /// Useful for validating whether a creature can gain a feat before attempting to add it.
        /// Returns false if the creature already has the feat or doesn't meet the requirements.
        /// </remarks>
        public static bool GetMeetsFeatRequirements(uint creature, FeatType feat)
        {
            var result = global::NWN.Core.NWNX.CreaturePlugin.GetMeetsFeatRequirements(creature, (int)feat);
            return result != 0;
        }

        /// <summary>
        /// Retrieves a special ability from the creature's special ability list by index.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="index">The index of the special ability. Must be between 0 and GetSpecialAbilityCount() - 1.</param>
        /// <returns>A SpecialAbilitySlot struct containing the special ability information.</returns>
        /// <remarks>
        /// Special abilities are unique powers or skills that creatures can possess.
        /// The returned struct contains the ability ID, level, and ready status.
        /// Use GetSpecialAbilityCount() to determine the valid index range.
        /// </remarks>
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
        /// Retrieves the total number of special abilities possessed by the creature.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The total number of special abilities in the creature's ability list.</returns>
        /// <remarks>
        /// This returns the count of all special abilities the creature possesses.
        /// Special abilities include racial powers, class features, and other unique capabilities.
        /// Use GetSpecialAbility() to retrieve specific abilities from this list.
        /// </remarks>
        public static int GetSpecialAbilityCount(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetSpecialAbilityCount(creature);
        }

        /// <summary>
        /// Adds a special ability to the creature's special ability list.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="ability">A SpecialAbilitySlot struct containing the ability information to add.</param>
        /// <remarks>
        /// This adds a new special ability to the creature's ability list.
        /// The ability will be immediately available to the creature.
        /// The SpecialAbilitySlot struct should contain valid ID, level, and ready status.
        /// </remarks>
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
        /// Removes a special ability from the creature's special ability list by index.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="index">The index of the special ability to remove. Must be between 0 and GetSpecialAbilityCount() - 1.</param>
        /// <remarks>
        /// This removes the special ability at the specified index from the creature's ability list.
        /// The creature will immediately lose access to the ability and its benefits.
        /// Use GetSpecialAbilityCount() to determine the valid index range.
        /// </remarks>
        public static void RemoveSpecialAbility(uint creature, int index)
        {
            global::NWN.Core.NWNX.CreaturePlugin.RemoveSpecialAbility(creature, index);
        }

        /// <summary>
        /// Modifies a special ability at the specified index in the creature's special ability list.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="index">The index of the special ability to modify. Must be between 0 and GetSpecialAbilityCount() - 1.</param>
        /// <param name="ability">A SpecialAbilitySlot struct containing the new ability information.</param>
        /// <remarks>
        /// This replaces the special ability at the specified index with the new ability information.
        /// The changes take effect immediately.
        /// Use GetSpecialAbilityCount() to determine the valid index range.
        /// </remarks>
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
        /// Retrieves the class taken by the creature at a specific level.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="level">The level to check. Must be a positive integer.</param>
        /// <returns>The class type taken at the specified level.</returns>
        /// <remarks>
        /// This returns the class that the creature gained at the specified level.
        /// Useful for tracking multiclass progression and level-based class features.
        /// Returns the class type as defined in the ClassType enum.
        /// </remarks>
        public static ClassType GetClassByLevel(uint creature, int level)
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
        public static void SetBaseAC(uint creature, int ac)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetBaseAC(creature, ac);
        }

        /// <summary>
        /// Retrieves the base armor class (AC) of the creature.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The creature's base AC value.</returns>
        /// <remarks>
        /// This returns the creature's base armor class before modifications from armor, dexterity, etc.
        /// The base AC is the foundation for the total AC calculation.
        /// Use SetBaseAC() to modify this value.
        /// </remarks>
        public static int GetBaseAC(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetBaseAC(creature);
        }

        /// <summary>
        /// Sets the raw ability score of the creature to a specific value.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="ability">The ability type to modify. See AbilityType enum for available abilities.</param>
        /// <param name="value">The raw ability score value to set. Must be between 1 and 255.</param>
        /// <remarks>
        /// This sets the creature's raw ability score without applying racial bonuses or penalties.
        /// Raw ability scores are the base values before any racial or other modifications.
        /// This directly affects derived statistics like saving throws and skill modifiers.
        /// </remarks>
        public static void SetRawAbilityScore(uint creature, AbilityType ability, int value)
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
        public static int GetRawAbilityScore(uint creature, AbilityType ability)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetRawAbilityScore(creature, (int)ability);
        }

        /// <summary>
        /// Modifies the raw ability score of the creature by adding a modifier.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="ability">The ability type to modify. See AbilityType enum for available abilities.</param>
        /// <param name="modifier">The modifier to apply to the ability score. Can be positive or negative.</param>
        /// <remarks>
        /// This adjusts the creature's raw ability score by the specified modifier amount.
        /// The modifier is added to the current raw ability score.
        /// This affects derived statistics like saving throws and skill modifiers.
        /// </remarks>
        public static void ModifyRawAbilityScore(uint creature, AbilityType ability, int modifier)
        {
            global::NWN.Core.NWNX.CreaturePlugin.ModifyRawAbilityScore(creature, (int)ability, modifier);
        }

        /// <summary>
        /// Retrieves the raw ability score a polymorphed creature had before polymorphing.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="ability">The ability type to query. Only Strength, Dexterity, and Constitution are valid.</param>
        /// <returns>The pre-polymorph raw ability score value.</returns>
        /// <remarks>
        /// This returns the creature's original ability score before polymorph effects were applied.
        /// Only works for Strength, Dexterity, and Constitution abilities.
        /// Useful for tracking original stats when creatures are polymorphed.
        /// Returns 0 if the creature was not polymorphed or for invalid ability types.
        /// </remarks>
        public static int GetPrePolymorphAbilityScore(uint creature, AbilityType ability)
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
        public static int GetRemainingSpellSlots(uint creature, ClassType classId, int level)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetRemainingSpellSlots(creature, (int)classId, level);
        }

        /// <summary>
        /// Sets the remaining spell slots for the creature's innate casting ability.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="classId">The class ID to modify. See ClassType enum for available classes.</param>
        /// <param name="level">The spell level to modify. Must be between 0 and 9.</param>
        /// <param name="slots">The number of spell slots to set. Must be non-negative.</param>
        /// <remarks>
        /// This sets the number of spell slots the creature has available for casting spells.
        /// The value cannot exceed the maximum spell slots for the class and level.
        /// Use GetMaxSpellSlots() to check the maximum allowed value.
        /// </remarks>
        public static void SetRemainingSpellSlots(uint creature, ClassType classId, int level, int slots)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetRemainingSpellSlots(creature, (int)classId, level, slots);
        }

        /// <summary>
        /// Removes a spell from the creature's spellbook for a specific class.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="classId">The class ID to modify. See ClassType enum for available classes.</param>
        /// <param name="level">The spell level to modify. Must be between 0 and 9.</param>
        /// <param name="spellId">The spell ID to remove. Must be a valid spell ID.</param>
        /// <remarks>
        /// This removes the specified spell from the creature's known spells for the given class and level.
        /// The creature will no longer be able to cast this spell.
        /// Use AddKnownSpell() to add spells to the creature's spellbook.
        /// </remarks>
        public static void RemoveKnownSpell(uint creature, ClassType classId, int level, int spellId)
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
        public static void AddKnownSpell(uint creature, ClassType classId, int level, int spellId)
        {
            global::NWN.Core.NWNX.CreaturePlugin.AddKnownSpell(creature, (int)classId, level, spellId);
        }

        /// <summary>
        /// Retrieves the maximum number of spell slots for the creature's class and level.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="classId">The class ID to check. See ClassType enum for available classes.</param>
        /// <param name="level">The spell level to check. Must be between 0 and 9.</param>
        /// <returns>The maximum number of spell slots for the specified class and level.</returns>
        /// <remarks>
        /// This returns the maximum number of spell slots the creature can have for the given class and level.
        /// This is determined by the creature's class levels and ability scores.
        /// Use SetRemainingSpellSlots() to set the current spell slots (cannot exceed this maximum).
        /// </remarks>
        public static int GetMaxSpellSlots(uint creature, ClassType classId, int level)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetMaxSpellSlots(creature, (int)classId, level);
        }

        /// <summary>
        /// Retrieves the maximum hit points for the creature at a specific level.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="level">The level to check. Must be a positive integer.</param>
        /// <returns>The maximum hit points for the specified level.</returns>
        /// <remarks>
        /// This returns the maximum hit points the creature gained at the specified level.
        /// This is useful for tracking hit point progression and level-based health increases.
        /// Use SetMaxHitPointsByLevel() to modify this value.
        /// </remarks>
        public static int GetMaxHitPointsByLevel(uint creature, int level)
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
        public static void SetMaxHitPointsByLevel(uint creature, int level, int value)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetMaxHitPointsByLevel(creature, level, value);
        }

        /// <summary>
        /// Sets the creature's base movement rate.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="rate">The movement rate to set. See MovementRate enum for available rates.</param>
        /// <remarks>
        /// This sets the creature's base movement rate, which determines how fast they can move.
        /// The movement rate affects walking, running, and other movement actions.
        /// Use GetMovementRateFactor() to get the current movement rate factor.
        /// </remarks>
        public static void SetMovementRate(uint creature, MovementRate rate)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetMovementRate(creature, (int)rate);
        }

        /// <summary>
        /// Retrieves the creature's current movement rate factor.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The movement rate factor. Base value is 1.0.</returns>
        /// <remarks>
        /// This returns the creature's current movement rate factor, which multiplies their base movement rate.
        /// A factor of 1.0 represents normal movement speed.
        /// Factors greater than 1.0 increase movement speed, while factors less than 1.0 decrease it.
        /// Use SetMovementRateFactor() to modify this value.
        /// </remarks>
        public static float GetMovementRateFactor(uint creature)
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
        public static void SetMovementRateFactor(uint creature, float factor)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetMovementRateFactor(creature, factor);
        }

        /// <summary>
        /// Sets the creature's raw good/evil alignment value.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="value">The good/evil alignment value. Typically ranges from -100 (evil) to +100 (good).</param>
        /// <remarks>
        /// This sets the creature's raw good/evil alignment value without any modifications.
        /// The alignment affects how the creature is perceived by others and may influence certain game mechanics.
        /// Use SetAlignmentLawChaos() to set the law/chaos alignment component.
        /// </remarks>
        public static void SetAlignmentGoodEvil(uint creature, int value)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetAlignmentGoodEvil(creature, value);
        }

        /// <summary>
        /// Sets the creature's raw law/chaos alignment value.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="value">The law/chaos alignment value. Typically ranges from -100 (chaotic) to +100 (lawful).</param>
        /// <remarks>
        /// This sets the creature's raw law/chaos alignment value without any modifications.
        /// The alignment affects how the creature is perceived by others and may influence certain game mechanics.
        /// Use SetAlignmentGoodEvil() to set the good/evil alignment component.
        /// </remarks>
        public static void SetAlignmentLawChaos(uint creature, int value)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetAlignmentLawChaos(creature, value);
        }

        /// <summary>
        /// Sets the base skill rank for the creature in a specific skill.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="skill">The skill type to modify. See NWNSkillType enum for available skills.</param>
        /// <param name="rank">The skill rank to set. Must be non-negative.</param>
        /// <remarks>
        /// This sets the creature's base skill rank in the specified skill.
        /// Skill ranks determine the creature's proficiency in various abilities and tasks.
        /// Higher ranks provide better chances of success in skill-based actions.
        /// </remarks>
        public static void SetSkillRank(uint creature, NWNSkillType skill, int rank)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetSkillRank(creature, (int)skill, rank);
        }

        /// <summary>
        /// Sets the class ID in a specific position for the creature.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="position">The class position to modify. Must be 0, 1, or 2.</param>
        /// <param name="classId">The class ID to set. Must be a valid ID from classes.2da (0-255).</param>
        /// <param name="updateLevels">Whether to update the creature's levels after the change. Default is true.</param>
        /// <remarks>
        /// This sets the creature's class at the specified position (0, 1, or 2).
        /// This is useful for multiclass creatures or changing a creature's primary class.
        /// If updateLevels is true, the creature's levels will be recalculated based on the new class.
        /// </remarks>
        public static void SetClassByPosition(uint creature, int position, ClassType classId, bool updateLevels = true)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetClassByPosition(creature, position, (int)classId, updateLevels ? 1 : 0);
        }

        /// <summary>
        /// Sets the creature's base attack bonus (BAB).
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="bab">The base attack bonus value to set. Must be between 0 and 254.</param>
        /// <remarks>
        /// This sets the creature's base attack bonus, which affects their attack rolls and attacks per round.
        /// Modifying the BAB will also affect the creature's attacks per round and eligibility for feats and prestige classes.
        /// Setting BAB to 0 will cause the creature to revert to its original BAB based on classes and levels.
        /// A creature can never have an actual BAB of zero.
        /// Note: The base game's SetBaseAttackBonus() function actually sets bonus attacks per round, not the BAB.
        /// </remarks>
        public static void SetBaseAttackBonus(uint creature, int bab)
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
        public static int GetAttacksPerRound(uint creature, bool bBaseAPR)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetAttacksPerRound(creature, bBaseAPR ? 1 : 0);
        }

        /// <summary>
        /// Restores all feat uses for the creature.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <remarks>
        /// This restores all feat uses for the creature, effectively resetting their feats to full capacity.
        /// This is useful for daily rest periods or when restoring creature abilities.
        /// The creature will regain all uses of their feats that have limited uses per day.
        /// </remarks>
        public static void RestoreFeats(uint creature)
        {
            global::NWN.Core.NWNX.CreaturePlugin.RestoreFeats(creature);
        }

        /// <summary>
        /// Restores all special ability uses for the creature.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <remarks>
        /// This restores all special ability uses for the creature, effectively resetting their abilities to full capacity.
        /// This is useful for daily rest periods or when restoring creature abilities.
        /// The creature will regain all uses of their special abilities that have limited uses per day.
        /// </remarks>
        public static void RestoreSpecialAbilities(uint creature)
        {
            global::NWN.Core.NWNX.CreaturePlugin.RestoreSpecialAbilities(creature);
        }

        /// <summary>
        /// Restores uses for all items carried by the creature.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <remarks>
        /// This restores all item uses for items carried by the creature, effectively resetting their items to full capacity.
        /// This is useful for daily rest periods or when restoring creature equipment.
        /// The creature will regain all uses of their items that have limited uses per day.
        /// </remarks>
        public static void RestoreItems(uint creature)
        {
            global::NWN.Core.NWNX.CreaturePlugin.RestoreItems(creature);
        }

        /// <summary>
        /// Sets the creature's size category.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="creatureSize">The creature size to set. See CreatureSize enum for available sizes.</param>
        /// <remarks>
        /// This sets the creature's size category, which affects various game mechanics including:
        /// - Attack rolls and armor class modifiers
        /// - Carrying capacity
        /// - Combat reach and positioning
        /// - Certain spell effects and area of effect calculations
        /// Use CREATURE_SIZE_* constants for valid size values.
        /// </remarks>
        public static void SetSize(uint creature, CreatureSize creatureSize)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetSize(creature, (int)creatureSize);
        }

        /// <summary>
        /// Retrieves the creature's remaining unspent skill points.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The number of remaining skill points available for spending.</returns>
        /// <remarks>
        /// This returns the number of skill points the creature has available to spend on skills.
        /// Skill points are typically gained through level advancement and can be spent to improve skill ranks.
        /// Use SetSkillPointsRemaining() to modify this value.
        /// </remarks>
        public static int GetSkillPointsRemaining(uint creature)
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
        public static void SetSkillPointsRemaining(uint creature, int skillpoints)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetSkillPointsRemaining(creature, skillpoints);
        }

        /// <summary>
        /// Sets the creature's racial type.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="racialtype">The racial type to set. See RacialType enum for available types.</param>
        /// <remarks>
        /// This sets the creature's racial type, which determines their racial abilities, bonuses, and penalties.
        /// The racial type affects various game mechanics including:
        /// - Racial ability score bonuses and penalties
        /// - Racial feats and special abilities
        /// - Size and movement characteristics
        /// - Certain spell effects and immunities
        /// </remarks>
        public static void SetRacialType(uint creature, RacialType racialtype)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetRacialType(creature, (int)racialtype);
        }

        /// <summary>
        /// Retrieves the creature's current movement type.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The creature's current movement type. See MovementType enum for possible values.</returns>
        /// <remarks>
        /// This returns the creature's current movement type, which determines how they can move.
        /// Movement types include walking, flying, swimming, and other specialized movement modes.
        /// The movement type affects the creature's ability to traverse different terrain types.
        /// </remarks>
        public static MovementType GetMovementType(uint creature)
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
        public static void SetWalkRateCap(uint creature, float fWalkRate)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetWalkRateCap(creature, fWalkRate);
        }

        /// <summary>
        /// Sets the creature's gold amount without sending a feedback message.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="gold">The gold amount to set. Must be non-negative.</param>
        /// <remarks>
        /// This sets the creature's gold amount without displaying any feedback message to the player.
        /// This is useful for administrative functions or when you want to modify gold silently.
        /// The gold amount cannot be negative.
        /// </remarks>
        public static void SetGold(uint creature, int gold)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetGold(creature, gold);
        }

        /// <summary>
        /// Sets the corpse decay time for the creature in milliseconds.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="decayTimeMs">The decay time in milliseconds. Must be non-negative.</param>
        /// <remarks>
        /// This sets how long the creature's corpse will remain in the world after death before disappearing.
        /// The decay time is specified in milliseconds.
        /// Setting to 0 means the corpse will never decay naturally.
        /// This affects the visual persistence of the creature's body after death.
        /// </remarks>
        public static void SetCorpseDecayTime(uint creature, int decayTimeMs)
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
        public static int GetBaseSavingThrow(uint creature, int which)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetBaseSavingThrow(creature, which);
        }

        /// <summary>
        /// Sets the base saving throw value for the creature.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="which">The saving throw type to modify. See SavingThrow enum for available types.</param>
        /// <param name="value">The base saving throw value to set. Must be non-negative.</param>
        /// <remarks>
        /// This sets the creature's base saving throw value for the specified type.
        /// The base saving throw is the foundation for the total saving throw calculation.
        /// This value is modified by ability scores and other factors to determine the final saving throw.
        /// </remarks>
        public static void SetBaseSavingThrow(uint creature, SavingThrow which, int value)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetBaseSavingThrow(creature, (int)which, value);
        }

        /// <summary>
        /// Adds the specified number of levels of the given class to the creature, bypassing all validation.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="classId">The class ID to add levels in. See ClassType enum for available classes.</param>
        /// <param name="count">The number of levels to add. Must be positive. Default is 1.</param>
        /// <remarks>
        /// This adds levels to the creature without any validation checks.
        /// This will not work on player characters - only NPCs and other creatures.
        /// The creature will gain all benefits of the new levels including hit points, feats, and abilities.
        /// Use LevelDown() to remove levels if needed.
        /// </remarks>
        public static void LevelUp(uint creature, ClassType classId, int count = 1)
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
        public static void LevelDown(uint creature, int count = 1)
        {
            global::NWN.Core.NWNX.CreaturePlugin.LevelDown(creature, count);
        }

        /// <summary>
        /// Sets the creature's challenge rating.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="fCR">The challenge rating to set. Must be non-negative.</param>
        /// <remarks>
        /// This sets the creature's challenge rating, which is used to determine encounter difficulty.
        /// The challenge rating affects experience point rewards and is used for encounter balancing.
        /// Higher challenge ratings indicate more difficult creatures.
        /// </remarks>
        public static void SetChallengeRating(uint creature, float fCR)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetChallengeRating(creature, fCR);
        }

        /// <summary>
        /// Retrieves the creature's highest attack bonus based on its own stats.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="isMelee">True to get melee/unarmed attack bonus, false to get ranged attack bonus. Default is true.</param>
        /// <param name="isTouchAttack">Whether this is a touch attack. Default is false.</param>
        /// <param name="isOffhand">Whether this is an offhand attack. Default is false.</param>
        /// <param name="includeBaseAttackBonus">Whether to include base attack bonus. Default is true.</param>
        /// <returns>The attack bonus value.</returns>
        /// <remarks>
        /// This returns the creature's highest attack bonus based on their own statistics.
        /// The bonus is calculated from the creature's base attack bonus, ability scores, and other factors.
        /// Note: AB vs. &lt;Type&gt; and +AB on Gauntlets are excluded from this calculation.
        /// </remarks>
        public static int GetAttackBonus(uint creature, bool isMelee = true, bool isTouchAttack = false,
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
        public static int GetHighestLevelOfFeat(uint creature, int feat)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetHighestLevelOfFeat(creature, feat);
        }

        /// <summary>
        /// Retrieves the remaining uses of a feat for the creature.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="feat">The feat type to check. See FeatType enum for available feats.</param>
        /// <returns>The number of remaining uses for the specified feat.</returns>
        /// <remarks>
        /// This returns the number of times the creature can still use the specified feat.
        /// Some feats have limited uses per day, and this tracks how many uses remain.
        /// Returns 0 if the creature doesn't have the feat or has no remaining uses.
        /// </remarks>
        public static int GetFeatRemainingUses(uint creature, FeatType feat)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetFeatRemainingUses(creature, (int)feat);
        }

        /// <summary>
        /// Retrieves the total uses of a feat for the creature.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="feat">The feat type to check. See FeatType enum for available feats.</param>
        /// <returns>The total number of uses for the specified feat.</returns>
        /// <remarks>
        /// This returns the total number of times the creature can use the specified feat per day.
        /// This is the maximum number of uses before the feat needs to be restored.
        /// Returns 0 if the creature doesn't have the feat or the feat has unlimited uses.
        /// </remarks>
        public static int GetFeatTotalUses(uint creature, FeatType feat)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetFeatTotalUses(creature, (int)feat);
        }

        /// <summary>
        /// Sets the remaining uses of a feat for the creature.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="feat">The feat type to modify. See FeatType enum for available feats.</param>
        /// <param name="uses">The number of remaining uses to set. Must be non-negative.</param>
        /// <remarks>
        /// This sets the number of remaining uses for the specified feat.
        /// The value cannot exceed the total uses for the feat.
        /// This is useful for restoring feat uses or setting specific usage amounts.
        /// </remarks>
        public static void SetFeatRemainingUses(uint creature, FeatType feat, int uses)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetFeatRemainingUses(creature, (int)feat, uses);
        }

        /// <summary>
        /// Retrieves the total effect bonus for the creature.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="bonusType">The bonus type to calculate. See BonusType enum for available types. Default is Attack.</param>
        /// <param name="target">The target object for the bonus calculation. Use OBJECT_INVALID for general bonuses. Default is OBJECT_INVALID.</param>
        /// <param name="isElemental">Whether this is an elemental bonus. Default is false.</param>
        /// <param name="isForceMax">Whether to force maximum bonus calculation. Default is false.</param>
        /// <param name="saveType">The save type for saving throw bonuses. Use -1 for general bonuses. Default is -1.</param>
        /// <param name="saveSpecificType">The specific save type for saving throw bonuses. Use -1 for general bonuses. Default is -1.</param>
        /// <param name="skill">The skill type for skill bonuses. Use NWNSkillType.Invalid for general bonuses. Default is Invalid.</param>
        /// <param name="abilityScore">The ability score for ability bonuses. Use -1 for general bonuses. Default is -1.</param>
        /// <param name="isOffhand">Whether this is an offhand bonus. Default is false.</param>
        /// <returns>The total effect bonus value.</returns>
        /// <remarks>
        /// This calculates the total effect bonus for the creature based on all active effects.
        /// The bonus type determines what kind of bonus to calculate (attack, damage, saving throw, etc.).
        /// Additional parameters allow for more specific bonus calculations.
        /// </remarks>
        public static int GetTotalEffectBonus(uint creature, BonusType bonusType = BonusType.Attack,
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
        public static void SetOriginalName(uint creature, string name, bool isLastName)
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
        public static string GetOriginalName(uint creature, bool isLastName)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetOriginalName(creature, isLastName ? 1 : 0);
        }

        /// <summary>
        /// Sets the creature's spell resistance value.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="sr">The spell resistance value to set. Must be non-negative.</param>
        /// <remarks>
        /// This sets the creature's spell resistance, which provides a chance to resist spell effects.
        /// Higher values provide better resistance to spells.
        /// Spell resistance is checked when spells are cast on the creature.
        /// </remarks>
        public static void SetSpellResistance(uint creature, int sr)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetSpellResistance(creature, sr);
        }

        /// <summary>
        /// Sets the creature's animal companion's name.
        /// </summary>
        /// <param name="creature">The master creature object. Must be a valid creature with an animal companion.</param>
        /// <param name="name">The name to set for the animal companion. Cannot be null or empty.</param>
        /// <remarks>
        /// This sets the name of the creature's animal companion.
        /// For player characters, this will persist to the .bic file if saved.
        /// A relog is required to see the name change take effect.
        /// Only works if the creature has an active animal companion.
        /// </remarks>
        public static void SetAnimalCompanionName(uint creature, string name)
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
        public static void SetFamiliarName(uint creature, string name)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetFamiliarName(creature, name);
        }

        /// <summary>
        /// Retrieves whether the creature can be disarmed.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>True if the creature can be disarmed, false otherwise.</returns>
        /// <remarks>
        /// This determines if the creature can have their weapons disarmed in combat.
        /// Some creatures or special abilities may prevent disarming.
        /// </remarks>
        public static bool GetDisarmable(uint creature)
        {
            var result = global::NWN.Core.NWNX.CreaturePlugin.GetDisarmable(creature);
            return result != 0;
        }

        /// <summary>
        /// Sets whether a creature can be disarmed.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="isDisarmable">True if the creature can be disarmed, false otherwise.</param>
        /// <remarks>
        /// This controls whether the creature can have their weapons disarmed in combat.
        /// Setting to false prevents disarming attempts from succeeding.
        /// </remarks>
        public static void SetDisarmable(uint creature, bool isDisarmable)
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
        public static void SetDomain(uint creature, ClassType @class, int index, int domain)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetDomain(creature, (int)@class, index, domain);
        }

        /// <summary>
        /// Sets the creature's specialist school for a specific class.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="class">The class type from the ClassType enum.</param>
        /// <param name="school">The school ID to set. Must be a valid school from schools.2da.</param>
        /// <remarks>
        /// This sets the specialist school for the creature's class.
        /// Specialist schools provide bonuses to certain spell schools and restrictions on others.
        /// Only certain classes (like Wizard) can have specialist schools.
        /// </remarks>
        public static void SetSpecialization(uint creature, ClassType @class, int school)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetSpecialization(creature, (int)@class, school);
        }

        /// <summary>
        /// Sets the creature's faction to the specified faction ID.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="factionId">The faction ID to assign to the creature. Must be a valid faction ID.</param>
        /// <remarks>
        /// This changes the creature's faction membership.
        /// Faction determines how the creature interacts with other creatures and objects.
        /// Changing faction can affect combat behavior and AI responses.
        /// </remarks>
        public static void SetFaction(uint creature, int factionId)
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
        public static int GetFaction(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetFaction(creature);
        }

        /// <summary>
        /// Retrieves whether a creature is currently flat-footed.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>True if the creature is flat-footed, false otherwise.</returns>
        /// <remarks>
        /// A flat-footed creature loses its Dexterity bonus to AC and cannot make attacks of opportunity.
        /// This typically occurs when the creature is surprised or has not acted in combat yet.
        /// </remarks>
        public static bool GetFlatFooted(uint creature)
        {
            var result = global::NWN.Core.NWNX.CreaturePlugin.GetFlatFooted(creature);
            return result != 0;
        }

        /// <summary>
        /// Serializes the creature's quickbar to a base64 string.
        /// </summary>
        /// <param name="creature">The creature object to serialize. Must be a valid creature.</param>
        /// <returns>A base64 string representation of the creature's quickbar.</returns>
        /// <remarks>
        /// This creates a serialized version of the creature's quickbar that can be stored or transferred.
        /// The quickbar contains all the creature's hotkey assignments and spell/item shortcuts.
        /// Use DeserializeQuickbar() to restore the quickbar from this string.
        /// </remarks>
        public static string SerializeQuickbar(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.SerializeQuickbar(creature);
        }

        /// <summary>
        /// Deserializes a serialized quickbar for the creature.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="serializedQuickbar">A base64 string containing the quickbar data.</param>
        /// <returns>True if the quickbar was successfully deserialized, false otherwise.</returns>
        /// <remarks>
        /// This restores the creature's quickbar from a previously serialized string.
        /// The quickbar will be completely replaced with the deserialized version.
        /// Use SerializeQuickbar() to create the serialized string.
        /// </remarks>
        public static bool DeserializeQuickbar(uint creature, string serializedQuickbar)
        {
            var result = global::NWN.Core.NWNX.CreaturePlugin.DeserializeQuickbar(creature, serializedQuickbar);
            return result != 0;
        }


        /// <summary>
        /// Sets the encounter source of the creature.
        /// </summary>
        /// <param name="creature">The target creature object to modify. Must be a valid creature.</param>
        /// <param name="encounter">The source encounter object. Use OBJECT_INVALID to remove encounter association.</param>
        /// <remarks>
        /// This associates the creature with a specific encounter.
        /// Encounters are used for AI behavior and combat management.
        /// Setting to OBJECT_INVALID removes the encounter association.
        /// </remarks>
        public static void SetEncounter(uint creature, uint encounter)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetEncounter(creature, encounter);
        }

        /// <summary>
        /// Retrieves the encounter source of the creature.
        /// </summary>
        /// <param name="creature">The target creature object to query. Must be a valid creature.</param>
        /// <returns>The encounter object ID, or OBJECT_INVALID if no encounter is associated.</returns>
        /// <remarks>
        /// This returns the encounter object that the creature is associated with.
        /// Returns OBJECT_INVALID if the creature has no encounter association.
        /// </remarks>
        public static uint GetEncounter(uint creature)
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
        public static void OverrideDamageLevel(uint creature, int damageLevel)
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
        public static bool GetIsBartering(uint creature)
        {
            var result = global::NWN.Core.NWNX.CreaturePlugin.GetIsBartering(creature);
            return result != 0;
        }


        /// <summary>
        /// Sets the caster level for the last item used by the creature.
        /// </summary>
        /// <param name="creature">The creature who used the item. Must be a valid creature.</param>
        /// <param name="casterLevel">The desired caster level to set. Must be non-negative.</param>
        /// <remarks>
        /// This sets the caster level for spells cast from the last item used by the creature.
        /// Use this in a spell hook or spell event before the spell is cast to override the caster level.
        /// This affects the power and duration of spells cast from items.
        /// </remarks>
        public static void SetLastItemCasterLevel(uint creature, int casterLevel)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetLastItemCasterLevel(creature, casterLevel);
        }

        /// <summary>
        /// Retrieves the caster level of the last item used by the creature.
        /// </summary>
        /// <param name="creature">The creature who used the item. Must be a valid creature.</param>
        /// <returns>The caster level of the creature's last used item.</returns>
        /// <remarks>
        /// This returns the caster level that was set for the last item used by the creature.
        /// Returns 0 if no item has been used or if there's an error.
        /// </remarks>
        public static int GetLastItemCasterLevel(uint creature)
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
        public static int GetArmorClassVersus(uint attacked, uint versus, bool touch = false)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetArmorClassVersus(attacked, versus, touch ? 1 : 0);
        }

        /// <summary>
        /// Moves a creature to limbo (a special area for temporary storage).
        /// </summary>
        /// <param name="creature">The creature object to move. Must be a valid creature.</param>
        /// <remarks>
        /// This moves the creature to limbo, which is typically used for temporary storage.
        /// Creatures in limbo are not visible and cannot interact with the world.
        /// This is useful for temporarily removing creatures from the game world.
        /// </remarks>
        public static void JumpToLimbo(uint creature)
        {
            global::NWN.Core.NWNX.CreaturePlugin.JumpToLimbo(creature);
        }

        /// <summary>
        /// Sets the critical hit multiplier modifier for the creature.
        /// </summary>
        /// <param name="creature">The target creature object to modify. Must be a valid creature.</param>
        /// <param name="modifier">The modifier to apply to the critical hit multiplier. Can be positive or negative.</param>
        /// <param name="hand">The hand to apply the modifier to: 0 for all attacks, 1 for Mainhand, 2 for Offhand. Default is 0.</param>
        /// <param name="persist">Whether the modifier should persist to the .bic file if applicable. Default is false.</param>
        /// <param name="baseItemType">The base item type to apply the modifier to. Use BaseItem.Invalid for all types. Default is Invalid.</param>
        /// <remarks>
        /// This modifies the critical hit multiplier for the creature's attacks.
        /// The modifier is added to the base critical hit multiplier.
        /// Persistence is activated each server reset by first use of either 'SetCriticalMultiplier*' functions.
        /// Recommended to trigger on a dummy target OnModuleLoad to enable persistence.
        /// </remarks>
        public static void SetCriticalMultiplierModifier(uint creature, int modifier, int hand = 0, bool persist = false, BaseItem baseItemType = BaseItem.Invalid)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetCriticalMultiplierModifier(creature, modifier, hand, persist ? 1 : 0, (int)baseItemType);
        }

        /// <summary>
        /// Retrieves the critical hit multiplier modifier for the creature.
        /// </summary>
        /// <param name="creature">The target creature object to query. Must be a valid creature.</param>
        /// <param name="hand">The hand to query: 0 for all attacks, 1 for Mainhand, 2 for Offhand. Default is 0.</param>
        /// <param name="baseItemType">The base item type to query. Use BaseItem.Invalid for all types. Default is Invalid.</param>
        /// <returns>The current critical hit multiplier modifier for the creature.</returns>
        /// <remarks>
        /// This returns the current critical hit multiplier modifier for the specified hand and item type.
        /// Returns 0 if no modifier is set or if there's an error.
        /// </remarks>
        public static int GetCriticalMultiplierModifier(uint creature, int hand = 0, BaseItem baseItemType = BaseItem.Invalid)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetCriticalMultiplierModifier(creature, hand, (int)baseItemType);
        }

        /// <summary>
        /// Sets the critical hit multiplier override for the creature.
        /// </summary>
        /// <param name="creature">The target creature object to modify. Must be a valid creature.</param>
        /// <param name="override">The override value to set for the critical hit multiplier. Must be positive.</param>
        /// <param name="hand">The hand to apply the override to: 0 for all attacks, 1 for Mainhand, 2 for Offhand. Default is 0.</param>
        /// <param name="persist">Whether the override should persist to the .bic file if applicable. Default is false.</param>
        /// <param name="baseItemType">The base item type to apply the override to. Use BaseItem.Invalid for all types. Default is Invalid.</param>
        /// <remarks>
        /// This overrides the critical hit multiplier for the creature's attacks.
        /// Unlike modifiers, overrides completely replace the base critical hit multiplier.
        /// Persistence is activated each server reset by first use of either 'SetCriticalMultiplier*' functions.
        /// </remarks>
        public static void SetCriticalMultiplierOverride(uint creature, int @override, int hand = 0, bool persist = false, BaseItem baseItemType = BaseItem.Invalid)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetCriticalMultiplierOverride(creature, @override, hand, persist ? 1 : 0, (int)baseItemType);
        }

        /// <summary>
        /// Retrieves the critical hit multiplier override for the creature.
        /// </summary>
        /// <param name="creature">The target creature object to query. Must be a valid creature.</param>
        /// <param name="hand">The hand to query: 0 for all attacks, 1 for Mainhand, 2 for Offhand. Default is 0.</param>
        /// <param name="baseItemType">The base item type to query. Use BaseItem.Invalid for all types. Default is Invalid.</param>
        /// <returns>The current critical hit multiplier override for the creature.</returns>
        /// <remarks>
        /// This returns the current critical hit multiplier override for the specified hand and item type.
        /// Returns 0 if no override is set or if there's an error.
        /// </remarks>
        public static int GetCriticalMultiplierOverride(uint creature, int hand = 0, BaseItem baseItemType = BaseItem.Invalid)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetCriticalMultiplierOverride(creature, hand, (int)baseItemType);
        }

        /// <summary>
        /// Sets the critical range modifier for the creature.
        /// </summary>
        /// <param name="creature">The target creature object to modify. Must be a valid creature.</param>
        /// <param name="modifier">The modifier to apply to the critical range. Can be positive or negative.</param>
        /// <param name="hand">The hand to apply the modifier to: 0 for all attacks, 1 for Mainhand, 2 for Offhand. Default is 0.</param>
        /// <param name="persist">Whether the modifier should persist to the .bic file if applicable. Default is false.</param>
        /// <param name="baseItemType">The base item type to apply the modifier to. Use BaseItem.Invalid for all types. Default is Invalid.</param>
        /// <remarks>
        /// This modifies the critical hit range for the creature's attacks.
        /// The modifier is added to the base critical hit range (typically 20).
        /// A positive modifier increases the critical hit range, making critical hits more likely.
        /// </remarks>
        public static void SetCriticalRangeModifier(uint creature, int modifier, int hand = 0, bool persist = false, BaseItem baseItemType = BaseItem.Invalid)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetCriticalRangeModifier(creature, modifier, hand, persist ? 1 : 0, (int)baseItemType);
        }

        /// <summary>
        /// Retrieves the critical range modifier for the creature.
        /// </summary>
        /// <param name="creature">The target creature object to query. Must be a valid creature.</param>
        /// <param name="hand">The hand to query: 0 for all attacks, 1 for Mainhand, 2 for Offhand. Default is 0.</param>
        /// <param name="baseItemType">The base item type to query. Use BaseItem.Invalid for all types. Default is Invalid.</param>
        /// <returns>The current critical range modifier for the creature.</returns>
        /// <remarks>
        /// This returns the current critical range modifier for the specified hand and item type.
        /// Returns 0 if no modifier is set or if there's an error.
        /// </remarks>
        public static int GetCriticalRangeModifier(uint creature, int hand = 0, BaseItem baseItemType = BaseItem.Invalid)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetCriticalRangeModifier(creature, hand, (int)baseItemType);
        }

        /// <summary>
        /// Sets the critical range override for the creature.
        /// </summary>
        /// <param name="creature">The target creature object to modify. Must be a valid creature.</param>
        /// <param name="override">The override value to set for the critical range. Must be positive.</param>
        /// <param name="hand">The hand to apply the override to: 0 for all attacks, 1 for Mainhand, 2 for Offhand. Default is 0.</param>
        /// <param name="persist">Whether the override should persist to the .bic file if applicable. Default is false.</param>
        /// <param name="baseItemType">The base item type to apply the override to. Use BaseItem.Invalid for all types. Default is Invalid.</param>
        /// <remarks>
        /// This overrides the critical hit range for the creature's attacks.
        /// Unlike modifiers, overrides completely replace the base critical hit range.
        /// The override value represents the highest roll that can result in a critical hit.
        /// </remarks>
        public static void SetCriticalRangeOverride(uint creature, int @override, int hand = 0, bool persist = false, BaseItem baseItemType = BaseItem.Invalid)
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
        public static int GetCriticalRangeOverride(uint creature, int hand = 0, BaseItem baseItemType = BaseItem.Invalid)
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
        public static void AddAssociate(uint creature, uint associate, int associateType)
        {
            global::NWN.Core.NWNX.CreaturePlugin.AddAssociate(creature, associate, associateType);
        }

        /// <summary>
        /// Retrieves the walk animation ID of the creature.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The walk animation ID.</returns>
        /// <remarks>
        /// This returns the animation ID used for the creature's walking animation.
        /// Different creatures can have different walk animations.
        /// Returns 0 if the creature has no walk animation or if there's an error.
        /// </remarks>
        public static int GetWalkAnimation(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetWalkAnimation(creature);
        }

        /// <summary>
        /// Sets the walk animation of the creature.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="animation">The animation ID to set. Must be a valid animation ID.</param>
        /// <remarks>
        /// This changes the walk animation used by the creature.
        /// Different animation IDs correspond to different walking styles.
        /// The animation will be applied immediately to the creature.
        /// </remarks>
        public static void SetWalkAnimation(uint creature, int animation)
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
        public static void SetAttackRollOverride(uint creature, int roll, int modifier)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetAttackRollOverride(creature, roll, modifier);
        }

        /// <summary>
        /// Sets whether the creature can parry all attacks.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="parry">True if the creature can parry all attacks, false otherwise.</param>
        /// <remarks>
        /// This enables or disables the creature's ability to parry all incoming attacks.
        /// When enabled, the creature can parry any attack regardless of the attacker's skill.
        /// This is typically used for special creatures or magical effects.
        /// </remarks>
        public static void SetParryAllAttacks(uint creature, bool parry)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetParryAllAttacks(creature, parry ? 1 : 0);
        }

        /// <summary>
        /// Retrieves whether the creature has no permanent death.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>True if the creature has no permanent death, false otherwise.</returns>
        /// <remarks>
        /// This indicates whether the creature is immune to permanent death.
        /// Creatures with no permanent death cannot be permanently killed and will respawn.
        /// This is typically used for important NPCs or special creatures.
        /// </remarks>
        public static bool GetNoPermanentDeath(uint creature)
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
        public static void SetNoPermanentDeath(uint creature, bool noPermanentDeath)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetNoPermanentDeath(creature, noPermanentDeath ? 1 : 0);
        }

        /// <summary>
        /// Computes a safe location for the creature near the specified position.
        /// </summary>
        /// <param name="creature">The creature object to find a safe location for. Must be a valid creature.</param>
        /// <param name="position">The position to search around. Must be a valid 3D position.</param>
        /// <param name="radius">The radius to search within. Default is 20.0f.</param>
        /// <param name="walkStraightLineRequired">Whether the creature must be able to walk in a straight line to the location. Default is true.</param>
        /// <returns>A safe location vector, or the original position if no safe location is found.</returns>
        /// <remarks>
        /// This finds a safe location for the creature near the specified position.
        /// A safe location is one where the creature can be placed without collision or obstruction.
        /// If walkStraightLineRequired is true, the creature must be able to walk directly to the location.
        /// Returns the original position if no safe location is found within the radius.
        /// </remarks>
        public static Vector3 ComputeSafeLocation(uint creature, Vector3 position, float radius = 20.0f, bool walkStraightLineRequired = true)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.ComputeSafeLocation(creature, position, radius, walkStraightLineRequired ? 1 : 0);
        }

        /// <summary>
        /// Performs a perception update on the creature for the target creature.
        /// </summary>
        /// <param name="creature">The creature object to update perception for. Must be a valid creature.</param>
        /// <param name="targetCreature">The target creature to check perception against. Must be a valid creature.</param>
        /// <remarks>
        /// This forces a perception update for the creature regarding the target creature.
        /// This can cause the creature to detect or lose detection of the target creature.
        /// Useful for triggering perception changes or updating AI awareness.
        /// </remarks>
        public static void DoPerceptionUpdateOnCreature(uint creature, uint targetCreature)
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
        public static float GetPersonalSpace(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetPersonalSpace(creature);
        }

        /// <summary>
        /// Sets the personal space value of the creature.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="personalSpace">The personal space value to set in meters. Must be non-negative.</param>
        /// <remarks>
        /// This sets the creature's personal space radius.
        /// Personal space determines how close other creatures can get before the creature reacts.
        /// Larger values mean the creature needs more space around it.
        /// </remarks>
        public static void SetPersonalSpace(uint creature, float personalSpace)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetPersonalSpace(creature, personalSpace);
        }

        /// <summary>
        /// Retrieves the creature personal space value.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The creature personal space value in meters.</returns>
        /// <remarks>
        /// This returns the creature's personal space radius for interactions with other creatures.
        /// This is different from the general personal space and is specific to creature-to-creature interactions.
        /// </remarks>
        public static float GetCreaturePersonalSpace(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetCreaturePersonalSpace(creature);
        }

        /// <summary>
        /// Sets the creature personal space value.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="creaturePersonalSpace">The creature personal space value to set in meters. Must be non-negative.</param>
        /// <remarks>
        /// This sets the creature's personal space radius for interactions with other creatures.
        /// This is different from the general personal space and is specific to creature-to-creature interactions.
        /// </remarks>
        public static void SetCreaturePersonalSpace(uint creature, float creaturePersonalSpace)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetCreaturePersonalSpace(creature, creaturePersonalSpace);
        }

        /// <summary>
        /// Retrieves the height of the creature.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The height value in meters.</returns>
        /// <remarks>
        /// This returns the creature's height, which affects collision detection and visual appearance.
        /// The height is used for determining if the creature can fit in certain spaces.
        /// </remarks>
        public static float GetHeight(uint creature)
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
        public static void SetHeight(uint creature, float height)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetHeight(creature, height);
        }

        /// <summary>
        /// Retrieves the hit distance of the creature.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The hit distance value in meters.</returns>
        /// <remarks>
        /// This returns the creature's hit distance, which determines how close the creature needs to be to hit a target.
        /// This affects melee combat range and AI behavior.
        /// </remarks>
        public static float GetHitDistance(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetHitDistance(creature);
        }

        /// <summary>
        /// Sets the hit distance of the creature.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="hitDistance">The hit distance value to set in meters. Must be positive.</param>
        /// <remarks>
        /// This sets the creature's hit distance, which determines how close the creature needs to be to hit a target.
        /// This affects melee combat range and AI behavior.
        /// </remarks>
        public static void SetHitDistance(uint creature, float hitDistance)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetHitDistance(creature, hitDistance);
        }

        /// <summary>
        /// Retrieves the preferred attack distance of the creature.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The preferred attack distance value in meters.</returns>
        /// <remarks>
        /// This returns the creature's preferred attack distance, which determines the optimal range for attacking.
        /// This affects AI behavior and combat positioning.
        /// </remarks>
        public static float GetPreferredAttackDistance(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetPreferredAttackDistance(creature);
        }

        /// <summary>
        /// Sets the preferred attack distance of the creature.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="preferredAttackDistance">The preferred attack distance value to set in meters. Must be positive.</param>
        /// <remarks>
        /// This sets the creature's preferred attack distance, which determines the optimal range for attacking.
        /// This affects AI behavior and combat positioning.
        /// </remarks>
        public static void SetPreferredAttackDistance(uint creature, float preferredAttackDistance)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetPreferredAttackDistance(creature, preferredAttackDistance);
        }

        /// <summary>
        /// Retrieves the armor check penalty of the creature.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The armor check penalty value.</returns>
        /// <remarks>
        /// This returns the armor check penalty that affects skill checks and other abilities.
        /// Heavy armor typically imposes penalties to certain skills like Move Silently and Hide.
        /// The penalty is usually negative (reduces skill checks).
        /// </remarks>
        public static int GetArmorCheckPenalty(uint creature)
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetArmorCheckPenalty(creature);
        }

        /// <summary>
        /// Retrieves the shield check penalty of the creature.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The shield check penalty value.</returns>
        /// <remarks>
        /// This returns the shield check penalty that affects skill checks and other abilities.
        /// Shields typically impose penalties to certain skills like Move Silently and Hide.
        /// The penalty is usually negative (reduces skill checks).
        /// </remarks>
        public static int GetShieldCheckPenalty(uint creature)
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
        public static void SetBypassEffectImmunity(uint creature, int immunityType, int chance = 100, bool persist = false)
        {
            global::NWN.Core.NWNX.CreaturePlugin.SetBypassEffectImmunity(creature, immunityType, chance, persist ? 1 : 0);
        }

        /// <summary>
        /// Retrieves the bypass effect immunity for the creature.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="immunityType">The immunity type to query. See immunity types in game constants.</param>
        /// <returns>The bypass effect immunity chance value (0-100).</returns>
        /// <remarks>
        /// This returns the chance percentage that the creature can bypass the specified immunity type.
        /// Returns 0 if the creature cannot bypass the immunity type.
        /// </remarks>
        public static int GetBypassEffectImmunity(uint creature, int immunityType)        
        {
            return global::NWN.Core.NWNX.CreaturePlugin.GetBypassEffectImmunity(creature, immunityType);
        }

        /// <summary>
        /// Retrieves the creature's number of bonus spells for a specific class and spell level.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="multiClass">The character class position, starting at 0. Must be between 0 and 2.</param>
        /// <param name="spellLevel">The spell level to query, 0 to 9.</param>
        /// <returns>The number of bonus spells for the specified class and spell level.</returns>
        /// <remarks>
        /// This returns the number of bonus spells the creature has for the specified class and spell level.
        /// Bonus spells are additional spell slots granted by high ability scores.
        /// Returns 0 if the creature has no bonus spells for the specified parameters.
        /// </remarks>
        public static int GetNumberOfBonusSpells(uint creature, int multiClass, int spellLevel)
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
        public static void ModifyNumberBonusSpells(uint creature, int multiClass, int spellLevel, int delta)
        {
            global::NWN.Core.NWNX.CreaturePlugin.ModifyNumberBonusSpells(creature, multiClass, spellLevel, delta);
        }
    }
}