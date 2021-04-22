using System.Numerics;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Creature;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Core.NWNX
{
    public static class Creature
    {
        private const string PLUGIN_NAME = "NWNX_Creature";

        // Gives the provided creature the provided feat.
        public static void AddFeat(uint creature, FeatType feat)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "AddFeat");
            Internal.NativeFunctions.nwnxPushInt((int)feat);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Gives the provided creature the provided feat.
        // Adds the feat to the stat list at the provided level.
        public static void AddFeatByLevel(uint creature, FeatType feat, int level)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "AddFeatByLevel");
            Internal.NativeFunctions.nwnxPushInt(level);
            Internal.NativeFunctions.nwnxPushInt((int)feat);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Removes from the provided creature the provided feat.
        public static void RemoveFeat(uint creature, FeatType feat)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "RemoveFeat");
            Internal.NativeFunctions.nwnxPushInt((int)feat);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static bool GetKnowsFeat(uint creature, FeatType feat)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetKnowsFeat");
            Internal.NativeFunctions.nwnxPushInt((int)feat);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt() != 0;
        }

        // Returns the count of feats learned at the provided level.
        public static int GetFeatCountByLevel(uint creature, int level)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetFeatCountByLevel");
            Internal.NativeFunctions.nwnxPushInt(level);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Returns the feat learned at the provided level at the provided index.
        // Index bounds: 0 <= index < GetFeatCountByLevel(creature, level).
        public static FeatType GetFeatByLevel(uint creature, int level, int index)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetFeatByLevel");
            Internal.NativeFunctions.nwnxPushInt(index);
            Internal.NativeFunctions.nwnxPushInt(level);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            return (FeatType)Internal.NativeFunctions.nwnxPopInt();
        }

        // Returns the total number of feats known by creature
        public static int GetFeatCount(uint creature)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetFeatCount");
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Returns the creature's feat at a given index
        // Index bounds: 0 <= index < GetFeatCount(creature);
        public static FeatType GetFeatByIndex(uint creature, int index)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetFeatByIndex");
            Internal.NativeFunctions.nwnxPushInt(index);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            return (FeatType)Internal.NativeFunctions.nwnxPopInt();
        }

        // Returns TRUE if creature meets all requirements to take given feat
        public static bool GetMeetsFeatRequirements(uint creature, FeatType feat)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetMeetsFeatRequirements");
            Internal.NativeFunctions.nwnxPushInt((int)feat);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt() != 0;
        }

        // Returns the special ability of the provided creature at the provided index.
        // Index bounds: 0 <= index < GetSpecialAbilityCount(creature).
        public static SpecialAbilitySlot GetSpecialAbility(uint creature, int index)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetSpecialAbility");
            var ability = new SpecialAbilitySlot();
            Internal.NativeFunctions.nwnxPushInt(index);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            ability.Level = Internal.NativeFunctions.nwnxPopInt();
            ability.Ready = Internal.NativeFunctions.nwnxPopInt();
            ability.ID = Internal.NativeFunctions.nwnxPopInt();
            return ability;
        }

        // Returns the count of special ability count of the provided creature.
        public static int GetSpecialAbilityCount(uint creature)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetSpecialAbilityCount");
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Adds the provided special ability to the provided creature.
        public static void AddSpecialAbility(uint creature, SpecialAbilitySlot ability)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "AddSpecialAbility");
            Internal.NativeFunctions.nwnxPushInt(ability.ID);
            Internal.NativeFunctions.nwnxPushInt(ability.Ready);
            Internal.NativeFunctions.nwnxPushInt(ability.Level);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Removes the provided special ability from the provided creature.
        // Index bounds: 0 <= index < GetSpecialAbilityCount(creature).
        public static void RemoveSpecialAbility(uint creature, int index)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "RemoveSpecialAbility");
            Internal.NativeFunctions.nwnxPushInt(index);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Sets the special ability at the provided index for the provided creature to the provided ability.
        // Index bounds: 0 <= index < GetSpecialAbilityCount(creature).
        public static void SetSpecialAbility(uint creature, int index, SpecialAbilitySlot ability)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetSpecialAbility");
            Internal.NativeFunctions.nwnxPushInt(ability.ID);
            Internal.NativeFunctions.nwnxPushInt(ability.Ready);
            Internal.NativeFunctions.nwnxPushInt(ability.Level);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Returns the classId taken by the provided creature at the provided level.
        public static ClassType GetClassByLevel(uint creature, int level)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetClassByLevel");
            Internal.NativeFunctions.nwnxPushInt(level);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            return (ClassType)Internal.NativeFunctions.nwnxPopInt();
        }

        // Sets the base AC for the provided creature.
        public static void SetBaseAC(uint creature, int ac)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetBaseAC");
            Internal.NativeFunctions.nwnxPushInt(ac);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Returns the base AC for the provided creature.
        public static int GetBaseAC(uint creature)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetBaseAC");
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Sets the provided ability score of provided creature to the provided value. Does not apply racial bonuses/penalties.
        public static void SetRawAbilityScore(uint creature, AbilityType ability, int value)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetRawAbilityScore");
            Internal.NativeFunctions.nwnxPushInt(value);
            Internal.NativeFunctions.nwnxPushInt((int)ability);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Gets the provided ability score of provided creature. Does not apply racial bonuses/penalties.
        public static int GetRawAbilityScore(uint creature, AbilityType ability)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetRawAbilityScore");
            Internal.NativeFunctions.nwnxPushInt((int)ability);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Adjusts the provided ability score of a provided creature. Does not apply racial bonuses/penalties.
        public static void ModifyRawAbilityScore(uint creature, AbilityType ability, int modifier)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ModifyRawAbilityScore");
            Internal.NativeFunctions.nwnxPushInt(modifier);
            Internal.NativeFunctions.nwnxPushInt((int)ability);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Gets the raw ability score a polymorphed creature had prior to polymorphing. Str/Dex/Con only.
        public static int GetPrePolymorphAbilityScore(uint creature, AbilityType ability)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetPrePolymorphAbilityScore");
            Internal.NativeFunctions.nwnxPushInt((int)ability);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Gets the memorized spell of the provided creature for the provided class, level, and index.
        // Index bounds: 0 <= index < GetMemorizedSpellCountByLevel(creature, class, level).
        public static MemorizedSpellSlot GetMemorizedSpell(uint creature, ClassType classId, int level, int index)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetMemorisedSpell");
            var spell = new MemorizedSpellSlot();
            Internal.NativeFunctions.nwnxPushInt(index);
            Internal.NativeFunctions.nwnxPushInt(level);
            Internal.NativeFunctions.nwnxPushInt((int)classId);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            spell.Domain = Internal.NativeFunctions.nwnxPopInt();
            spell.Meta = Internal.NativeFunctions.nwnxPopInt();
            spell.Ready = Internal.NativeFunctions.nwnxPopInt();
            spell.ID = Internal.NativeFunctions.nwnxPopInt();
            return spell;
        }

        // Gets the count of memorized spells of the provided classId and level belonging to the provided creature.
        public static int GetMemorizedSpellCountByLevel(uint creature, ClassType classId, int level)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetMemorisedSpellCountByLevel");
            Internal.NativeFunctions.nwnxPushInt(level);
            Internal.NativeFunctions.nwnxPushInt((int)classId);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Sets the memorized spell of the provided creature for the provided class, level, and index.
        // Index bounds: 0 <= index < GetMemorizedSpellCountByLevel(creature, class, level).
        public static void SetMemorizedSpell(uint creature, ClassType classId, int level, int index,
            MemorizedSpellSlot spell)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetMemorisedSpell");
            Internal.NativeFunctions.nwnxPushInt(spell.ID);
            Internal.NativeFunctions.nwnxPushInt(spell.Ready);
            Internal.NativeFunctions.nwnxPushInt(spell.Meta);
            Internal.NativeFunctions.nwnxPushInt(spell.Domain);
            Internal.NativeFunctions.nwnxPushInt(index);
            Internal.NativeFunctions.nwnxPushInt(level);
            Internal.NativeFunctions.nwnxPushInt((int)classId);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Gets the remaining spell slots (innate casting) for the provided creature for the provided classId and level.
        public static int GetRemainingSpellSlots(uint creature, ClassType classId, int level)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetRemainingSpellSlots");
            Internal.NativeFunctions.nwnxPushInt(level);
            Internal.NativeFunctions.nwnxPushInt((int)classId);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Sets the remaining spell slots (innate casting) for the provided creature for the provided classId and level.
        public static void SetRemainingSpellSlots(uint creature, ClassType classId, int level, int slots)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetRemainingSpellSlots");
            Internal.NativeFunctions.nwnxPushInt(slots);
            Internal.NativeFunctions.nwnxPushInt(level);
            Internal.NativeFunctions.nwnxPushInt((int)classId);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Get the spell at index in level in creature's spellbook from class.
        public static int GetKnownSpell(uint creature, ClassType classId, int level, int index)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetKnownSpell");
            Internal.NativeFunctions.nwnxPushInt(index);
            Internal.NativeFunctions.nwnxPushInt(level);
            Internal.NativeFunctions.nwnxPushInt((int)classId);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        public static int GetKnownSpellCount(uint creature, ClassType classId, int level)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetKnownSpellCount");
            Internal.NativeFunctions.nwnxPushInt(level);
            Internal.NativeFunctions.nwnxPushInt((int)classId);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Remove a spell from creature's spellbook for class.
        public static void RemoveKnownSpell(uint creature, ClassType classId, int level, int spellId)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "RemoveKnownSpell");
            Internal.NativeFunctions.nwnxPushInt(spellId);
            Internal.NativeFunctions.nwnxPushInt(level);
            Internal.NativeFunctions.nwnxPushInt((int)classId);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Add a new spell to creature's spellbook for class.
        public static void AddKnownSpell(uint creature, ClassType classId, int level, int spellId)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "AddKnownSpell");
            Internal.NativeFunctions.nwnxPushInt(spellId);
            Internal.NativeFunctions.nwnxPushInt(level);
            Internal.NativeFunctions.nwnxPushInt((int)classId);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Gets the maximum count of spell slots for the proivded creature for the provided classId and level.
        public static int GetMaxSpellSlots(uint creature, ClassType classId, int level)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetMaxSpellSlots");
            Internal.NativeFunctions.nwnxPushInt(level);
            Internal.NativeFunctions.nwnxPushInt((int)classId);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Gets the maximum hit points for creature for level.
        public static int GetMaxHitPointsByLevel(uint creature, int level)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetMaxHitPointsByLevel");
            Internal.NativeFunctions.nwnxPushInt(level);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Sets the maximum hit points for creature for level to nValue.
        public static void SetMaxHitPointsByLevel(uint creature, int level, int value)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetMaxHitPointsByLevel");
            Internal.NativeFunctions.nwnxPushInt(value);
            Internal.NativeFunctions.nwnxPushInt(level);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Set creature's movement rate.
        public static void SetMovementRate(uint creature, MovementRate rate)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetMovementRate");
            Internal.NativeFunctions.nwnxPushInt((int)rate);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Returns the creature's current movement rate factor (base = 1.0)
        public static float GetMovementRateFactor(uint creature)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetMovementRateFactor");
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopFloat();
        }

        // Sets the creature's current movement rate factor (base = 1.0)
        public static void SetMovementRateFactor(uint creature, float factor)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetMovementRateFactor");
            Internal.NativeFunctions.nwnxPushFloat(factor);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Set creature's raw good/evil alignment value.
        public static void SetAlignmentGoodEvil(uint creature, int value)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetAlignmentGoodEvil");
            Internal.NativeFunctions.nwnxPushInt(value);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Set creature's raw law/chaos alignment value.
        public static void SetAlignmentLawChaos(uint creature, int value)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetAlignmentLawChaos");
            Internal.NativeFunctions.nwnxPushInt(value);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Get the soundset index for creature.
        public static int GetSoundset(uint creature)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetSoundset");
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Set the soundset index for creature.
        public static void SetSoundset(uint creature, int soundset)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetSoundset");
            Internal.NativeFunctions.nwnxPushInt(soundset);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Set the base ranks in a skill for creature
        public static void SetSkillRank(uint creature, Skill skill, int rank)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetSkillRank");
            Internal.NativeFunctions.nwnxPushInt(rank);
            Internal.NativeFunctions.nwnxPushInt((int)skill);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Set the classId ID in a particular position for a creature.
        // Position should be 0, 1, or 2.
        // ClassID should be a valid ID number in classes.2da and be between 0 and 255.
        public static void SetClassByPosition(uint creature, int position, ClassType classId)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetClassByPosition");
            Internal.NativeFunctions.nwnxPushInt((int)classId);
            Internal.NativeFunctions.nwnxPushInt(position);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Set creature's base attack bonus (BAB)
        // Modifying the BAB will also affect the creature's attacks per round and its
        // eligability for feats, prestige classes, etc.
        // The BAB value should be between 0 and 254.
        // Setting BAB to 0 will cause the creature to revert to its original BAB based
        // on its classes and levels. A creature can never have an actual BAB of zero.
        // NOTE: The base game has a function SetBaseAttackBonus(), which actually sets
        //       the bonus attacks per round for a creature, not the BAB.
        public static void SetBaseAttackBonus(uint creature, int bab)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetBaseAttackBonus");
            Internal.NativeFunctions.nwnxPushInt(bab);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Gets the creatures current attacks per round (using equipped weapon)
        // bBaseAPR - If true, will return the base attacks per round, based on BAB and
        //            equipped weapons, regardless of overrides set by
        //            calls to SetBaseAttackBonus() builtin function.
        public static int GetAttacksPerRound(uint creature, bool bBaseAPR)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetAttacksPerRound");
            Internal.NativeFunctions.nwnxPushInt(bBaseAPR ? 1 : 0);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Sets the creature gender
        public static void SetGender(uint creature, Gender gender)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetGender");
            Internal.NativeFunctions.nwnxPushInt((int)gender);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Restore all creature feat uses
        public static void RestoreFeats(uint creature)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "RestoreFeats");
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Restore all creature special ability uses
        public static void RestoreSpecialAbilities(uint creature)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "RestoreSpecialAbilities");
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Restore all creature spells per day for given level.
        // If level is -1, all spells are restored
        public static void RestoreSpells(uint creature, int level)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "RestoreSpells");
            Internal.NativeFunctions.nwnxPushInt(level);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Restore uses for all items carried by the creature
        public static void RestoreItems(uint creature)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "RestoreItems");
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Sets the creature size. Use CREATURE_SIZE_* constants
        public static void SetSize(uint creature, CreatureSize creatureSize)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetSize");
            Internal.NativeFunctions.nwnxPushInt((int)creatureSize);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Gets the creature's remaining unspent skill points
        public static int GetSkillPointsRemaining(uint creature)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetSkillPointsRemaining");
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }


        // Sets the creature's remaining unspent skill points
        public static void SetSkillPointsRemaining(uint creature, int skillpoints)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetSkillPointsRemaining");
            Internal.NativeFunctions.nwnxPushInt(skillpoints);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Sets the creature's racial type
        public static void SetRacialType(uint creature, RacialType racialtype)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetRacialType");
            Internal.NativeFunctions.nwnxPushInt((int)racialtype);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Returns the creature's current movement type (MOVEMENT_TYPE_*)
        public static MovementType GetMovementType(uint creature)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetMovementType");
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            return (MovementType)Internal.NativeFunctions.nwnxPopInt();
        }

        // Sets the maximum movement rate a creature can have while walking (not running)
        // This allows a creature with movement speed enhancemens to walk at a normal rate.
        // Setting the value to -1.0 will remove the cap.
        // Default value is 2000.0, which is the base human walk speed.
        public static void SetWalkRateCap(uint creature, float fWalkRate)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetWalkRateCap");
            Internal.NativeFunctions.nwnxPushFloat(fWalkRate);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Sets the creature's gold without sending a feedback message
        public static void SetGold(uint creature, int gold)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetGold");
            Internal.NativeFunctions.nwnxPushInt(gold);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Sets corpse decay time in milliseconds
        public static void SetCorpseDecayTime(uint creature, int decayTimeMs)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetCorpseDecayTime");
            Internal.NativeFunctions.nwnxPushInt(decayTimeMs);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Returns the creature's base save and any modifiers set in the toolset
        public static int GetBaseSavingThrow(uint creature, int which)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetBaseSavingThrow");
            Internal.NativeFunctions.nwnxPushInt(which);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Sets the base saving throw of the creature
        public static void SetBaseSavingThrow(uint creature, int which, int value)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetBaseSavingThrow");
            Internal.NativeFunctions.nwnxPushInt(value);
            Internal.NativeFunctions.nwnxPushInt(which);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Add count levels of class to the creature, bypassing all validation
        // This will not work on player characters
        public static void LevelUp(uint creature, ClassType classId, int count = 1)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "LevelUp");
            Internal.NativeFunctions.nwnxPushInt(count);
            Internal.NativeFunctions.nwnxPushInt((int)classId);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Remove last count levels from a creature
        // This will not work on player characters
        public static void LevelDown(uint creature, int count = 1)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "LevelDown");
            Internal.NativeFunctions.nwnxPushInt(count);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Sets the creature's challenge rating
        public static void SetChallengeRating(uint creature, float fCR)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetChallengeRating");
            Internal.NativeFunctions.nwnxPushFloat(fCR);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Returns the creature's highest attack bonus based on its own stats
        // NOTE: AB vs. <Type> and +AB on Gauntlets are excluded
        //
        // int isMelee values:
        //   TRUE: Get Melee/Unarmed Attack Bonus
        //   FALSE: Get Ranged Attack Bonus
        public static int GetAttackBonus(uint creature, bool isMelee = true, bool isTouchAttack = false,
            bool isOffhand = false, bool includeBaseAttackBonus = true)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetAttackBonus");
            Internal.NativeFunctions.nwnxPushInt(includeBaseAttackBonus ? 1 : 0);
            Internal.NativeFunctions.nwnxPushInt(isOffhand ? 1 : 0);
            Internal.NativeFunctions.nwnxPushInt(isTouchAttack ? 1 : 0);
            Internal.NativeFunctions.nwnxPushInt(isMelee ? 1 : 0);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Get highest level version of feat posessed by creature (e.g. for barbarian rage)
        public static int GetHighestLevelOfFeat(uint creature, int feat)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetHighestLevelOfFeat");
            Internal.NativeFunctions.nwnxPushInt(feat);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Get feat remaining uses of a creature
        public static int GetFeatRemainingUses(uint creature, FeatType feat)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetFeatRemainingUses");
            Internal.NativeFunctions.nwnxPushInt((int)feat);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Get feat total uses of a creature
        public static int GetFeatTotalUses(uint creature, FeatType feat)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetFeatTotalUses");
            Internal.NativeFunctions.nwnxPushInt((int)feat);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Set feat remaining uses of a creature
        public static void SetFeatRemainingUses(uint creature, FeatType feat, int uses)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetFeatRemainingUses");
            Internal.NativeFunctions.nwnxPushInt(uses);
            Internal.NativeFunctions.nwnxPushInt((int)feat);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Get total effect bonus
        public static int GetTotalEffectBonus(uint creature, BonusType bonusType = BonusType.Attack,
            uint target = NWScript.NWScript.OBJECT_INVALID, bool isElemental = false,
            bool isForceMax = false, int saveType = -1, int saveSpecificType = -1, Skill skill = Skill.Invalid,
            int abilityScore = -1, bool isOffhand = false)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetTotalEffectBonus");
            Internal.NativeFunctions.nwnxPushInt(isOffhand ? 1 : 0);
            Internal.NativeFunctions.nwnxPushInt(abilityScore);
            Internal.NativeFunctions.nwnxPushInt((int)skill);
            Internal.NativeFunctions.nwnxPushInt(saveSpecificType);
            Internal.NativeFunctions.nwnxPushInt(saveType);
            Internal.NativeFunctions.nwnxPushInt(isForceMax ? 1 : 0);
            Internal.NativeFunctions.nwnxPushInt(isElemental ? 1 : 0);
            Internal.NativeFunctions.nwnxPushObject(target);
            Internal.NativeFunctions.nwnxPushInt((int)bonusType);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Set the original first or last name of creature
        //
        // For PCs this will persist to the .bic file if saved. Requires a relog to update.
        public static void SetOriginalName(uint creature, string name, bool isLastName)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetOriginalName");
            Internal.NativeFunctions.nwnxPushInt(isLastName ? 1 : 0);
            Internal.NativeFunctions.nwnxPushString(name);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Get the original first or last name of creature
        public static string GetOriginalName(uint creature, bool isLastName)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetOriginalName");
            Internal.NativeFunctions.nwnxPushInt(isLastName ? 1 : 0);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopString();
        }

        // Set creature's spell resistance
        public static void SetSpellResistance(uint creature, int sr)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetSpellResistance");
            Internal.NativeFunctions.nwnxPushInt(sr);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Set creature's animal companion's name
        /// @param creature The master creature object.
        /// @param name The name to give their animal companion.
        public static void SetAnimalCompanionName(uint creature, string name)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetAnimalCompanionCreatureType");
            Internal.NativeFunctions.nwnxPushString(name);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Set creature's familiar's name
        /// @param creature The master creature object.
        /// @param name The name to give their familiar.
        public static void SetFamiliarName(uint creature, string name)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetFamiliarCreatureType");
            Internal.NativeFunctions.nwnxPushString(name);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Get whether the creature can be disarmed.
        /// @param creature The creature object.
        /// @return TRUE if the creature can be disarmed.
        public static int GetDisarmable(uint creature)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetDisarmable");
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        /// @brief Set whether a creature can be disarmed.
        /// @param creature The creature object.
        /// @param disarmable Set to TRUE if the creature can be disarmed.
        public static void SetDisarmable(uint creature, bool isDisarmable)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetDisarmable");
            Internal.NativeFunctions.nwnxPushInt(isDisarmable ? 1 : 0);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Sets one of creature's domains.
        /// @param creature The creature object.
        /// @param class The class id from classes.2da. (Not class index 0-2)
        /// @param index The first or second domain.
        /// @param domain The domain constant to set.
        public static void SetDomain(uint creature, ClassType @class, int index, int domain)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetDomain");
            Internal.NativeFunctions.nwnxPushInt(domain);
            Internal.NativeFunctions.nwnxPushInt(index);
            Internal.NativeFunctions.nwnxPushInt((int)@class);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Sets creature's specialist school.
        /// @param creature The creature object.
        /// @param class The class id from classes.2da. (Not class index 0-2)
        /// @param school The school constant.
        public static void SetSpecialization(uint creature, ClassType @class, int school)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetSpecialization");
            Internal.NativeFunctions.nwnxPushInt(school);
            Internal.NativeFunctions.nwnxPushInt((int)@class);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Sets oCreatures faction to be the faction with id nFactionId.
        /// @param oCreature The creature.
        /// @param nFactionId The faction id we want the creature to join.
        public static void SetFaction(uint creature, int factionId)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetFaction");
            Internal.NativeFunctions.nwnxPushInt(factionId);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Gets the faction id from oCreature
        /// @param oCreature the creature we wish to query against
        /// @return faction id as an integer, -1 when used against invalid creature or invalid object.
        public static int GetFaction(uint creature)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetFaction");
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        /// @brief Get whether a creature is flat-footed.
        /// @param The creature object.
        /// @return TRUE if the creature is flat-footed.
        public static bool GetFlatFooted(uint creature)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetFlatFooted");
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt() == 1;
        }

        /// @brief Serialize oCreature's quickbar to a base64 string
        /// @param oCreature The creature.
        /// @return A base64 string representation of oCreature's quickbar.
        public static string SerializeQuickbar(uint creature)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SerializeQuickbar");
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopString();
        }

        /// @brief Deserialize sSerializedQuickbar for oCreature
        /// @param oCreature The creature.
        /// @param sSerializedQuickbar A base64 string of a quickbar
        /// @return TRUE on success
        public static bool DeserializeQuickbar(uint creature, string serializedQuickbar)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "DeserializeQuickbar");
            Internal.NativeFunctions.nwnxPushString(serializedQuickbar);
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt() == 1;
        }


        /// @brief Set the encounter source of oCreature.
        /// @param oCreature The target creature.
        /// @param oEncounter The source encounter
        public static void SetEncounter(uint oCreature, uint oEncounter)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetEncounter");

            Internal.NativeFunctions.nwnxPushObject(oEncounter);
            Internal.NativeFunctions.nwnxPushObject(oCreature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Get the encounter source of oCreature.
        /// @param oCreature The target creature.
        /// @return The encounter, OBJECT_INVALID if not part of an encounter or on error
        public static uint GetEncounter(uint oCreature)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetEncounter");

            Internal.NativeFunctions.nwnxPushObject(oCreature);
            Internal.NativeFunctions.nwnxCallFunction();

            return Internal.NativeFunctions.nwnxPopObject();
        }


        /// @brief Set whether an effect icon is flashing or not.
        /// @param oCreature The target creature.
        /// @param nIconId The icon id, see effecticons.2da.
        /// @param bFlashing TRUE for flashing, FALSE for not flashing.
        public static void SetEffectIconFlashing(uint oCreature, int nIconId, bool bFlashing)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetEffectIconFlashing");

            Internal.NativeFunctions.nwnxPushInt(bFlashing ? 1 : 0);
            Internal.NativeFunctions.nwnxPushInt(nIconId);
            Internal.NativeFunctions.nwnxPushObject(oCreature);
            Internal.NativeFunctions.nwnxCallFunction();
        }


        /// @brief Override the damage level of oCreature.
        /// @note Damage levels are the damage state under a creature's name, for example: 'Near Death'
        /// @param oCreature The target creature.
        /// @param nDamageLevel A damage level, see damagelevels.2da. Allowed values: 0-255 or -1 to remove the override.
        public static void OverrideDamageLevel(uint oCreature, int nDamageLevel)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "OverrideDamageLevel");

            Internal.NativeFunctions.nwnxPushInt(nDamageLevel);
            Internal.NativeFunctions.nwnxPushObject(oCreature);
            Internal.NativeFunctions.nwnxCallFunction();
        }


        /// @brief Get if oCreature is currently bartering.
        /// @param oCreature The target creature.
        /// @return TRUE if oCreature is bartering, FALSE if not or on error.
        public static bool GetIsBartering(uint oCreature)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetIsBartering");

            Internal.NativeFunctions.nwnxPushObject(oCreature);
            Internal.NativeFunctions.nwnxCallFunction();

            return Internal.NativeFunctions.nwnxPopInt() == 1;
        }


        /// @brief Sets caster level for the last item used. Use in a spellhook or spell event before to set caster level for any spells cast from the item.
        /// @param oCreature the creature who used the item.
        /// @param nCasterLvl the desired caster level.
        public static void SetLastItemCasterLevel(uint oCreature, int nCasterLvl)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetLastItemCasterLevel");

            Internal.NativeFunctions.nwnxPushInt(nCasterLvl);
            Internal.NativeFunctions.nwnxPushObject(oCreature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Gets the caster level of the last item used.
        /// @param oCreature the creature who used the item.
        /// @return returns the creatures last used item's level.
        public static int GetLastItemCasterLevel(uint oCreature)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetLastItemCasterLevel");

            Internal.NativeFunctions.nwnxPushObject(oCreature);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        /// @brief Gets the Armor classed of attacked against versus
        /// @param oAttacked The one being attacked
        /// @param oVersus The one doing the attacking
        /// @param nTouch TRUE for touch attacks
        /// @return -255 on Error, Flat footed AC if oVersus is invalid or the Attacked AC versus oVersus.
        public static int GetArmorClassVersus(uint oAttacked, uint oVersus, bool nTouch = false)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetArmorClassVersus");

            Internal.NativeFunctions.nwnxPushInt(nTouch ? 1 : 0);
            Internal.NativeFunctions.nwnxPushObject(oVersus);
            Internal.NativeFunctions.nwnxPushObject(oAttacked);
            Internal.NativeFunctions.nwnxCallFunction();

            return Internal.NativeFunctions.nwnxPopInt();
        }

        /// @brief Move a creature to limbo.
        /// @param oCreature The creature object.
        public static void JumpToLimbo(uint oCreature)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "JumpToLimbo");
            Internal.NativeFunctions.nwnxPushObject(oCreature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Sets the critical hit multiplier modifier for the creature
        /// @param oCreature The target creature
        /// @param nModifier The modifier to apply
        /// @param nHand 0 for all attacks, 1 for Mainhand, 2 for Offhand
        /// @param bPersist Whether the modifier should persist to .bic file if applicable
        /// @note Persistence is activated each server reset by first use of either 'SetCriticalMultiplier*' functions. Recommended to trigger on a dummy target OnModuleLoad to enable persistence.
        public static void SetCriticalMultiplierModifier(uint oCreature, int nModifier, int nHand = 0, bool bPersist = false, BaseItem baseItemType = BaseItem.Invalid)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetCriticalMultiplierModifier");

            Internal.NativeFunctions.nwnxPushInt((int)baseItemType);
            Internal.NativeFunctions.nwnxPushInt(bPersist ? 1 : 0);
            Internal.NativeFunctions.nwnxPushInt(nHand);
            Internal.NativeFunctions.nwnxPushInt(nModifier);
            Internal.NativeFunctions.nwnxPushObject(oCreature);

            Internal.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Gets the critical hit multiplier modifier for the Creature
        /// @param oCreature The target creature
        /// @param nHand 0 for all attacks, 1 for Mainhand, 2 for Offhand
        /// @return the current critical hit multiplier modifier for the creature
        public static int GetCriticalMultiplierModifier(uint oCreature, int nHand = 0, BaseItem baseItemType = BaseItem.Invalid)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetCriticalMultiplierModifier");

            Internal.NativeFunctions.nwnxPushInt((int)baseItemType);
            Internal.NativeFunctions.nwnxPushInt(nHand);
            Internal.NativeFunctions.nwnxPushObject(oCreature);
            Internal.NativeFunctions.nwnxCallFunction();

            return Internal.NativeFunctions.nwnxPopInt();
        }

        public static void SetCriticalMultiplierOverride(uint oCreature, int nOverride, int nHand = 0, bool bPersist = false, BaseItem baseItemType = BaseItem.Invalid)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetCriticalMultiplierOverride");

            Internal.NativeFunctions.nwnxPushInt((int)baseItemType);
            Internal.NativeFunctions.nwnxPushInt(bPersist ? 1 : 0);
            Internal.NativeFunctions.nwnxPushInt(nHand);
            Internal.NativeFunctions.nwnxPushInt(nOverride);
            Internal.NativeFunctions.nwnxPushObject(oCreature);

            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static int GetCriticalMultiplierOverride(uint oCreature, int nHand = 0, BaseItem baseItemType = BaseItem.Invalid)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetCriticalMultiplierOverride");

            Internal.NativeFunctions.nwnxPushInt((int)baseItemType);
            Internal.NativeFunctions.nwnxPushInt(nHand);
            Internal.NativeFunctions.nwnxPushObject(oCreature);
            Internal.NativeFunctions.nwnxCallFunction();

            return Internal.NativeFunctions.nwnxPopInt();
        }

        public static void SetCriticalRangeModifier(uint oCreature, int nModifier, int nHand = 0, bool bPersist = false, BaseItem baseItemType  = BaseItem.Invalid)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetCriticalRangeModifier");

            Internal.NativeFunctions.nwnxPushInt((int)baseItemType);
            Internal.NativeFunctions.nwnxPushInt(bPersist ? 1 : 0);
            Internal.NativeFunctions.nwnxPushInt(nHand);
            Internal.NativeFunctions.nwnxPushInt(nModifier);
            Internal.NativeFunctions.nwnxPushObject(oCreature);

            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static int GetCriticalRangeModifier(uint oCreature, int nHand = 0, BaseItem baseItemType = BaseItem.Invalid)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetCriticalRangeModifier");

            Internal.NativeFunctions.nwnxPushInt((int)baseItemType);
            Internal.NativeFunctions.nwnxPushInt(nHand);
            Internal.NativeFunctions.nwnxPushObject(oCreature);
            Internal.NativeFunctions.nwnxCallFunction();

            return Internal.NativeFunctions.nwnxPopInt();
        }

        public static void SetCriticalRangeOverride(uint oCreature, int nOverride, int nHand = 0, bool bPersist = false, BaseItem baseItemType = BaseItem.Invalid)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetCriticalRangeOverride");

            Internal.NativeFunctions.nwnxPushInt((int)baseItemType);
            Internal.NativeFunctions.nwnxPushInt(bPersist ? 1 : 0);
            Internal.NativeFunctions.nwnxPushInt(nHand);
            Internal.NativeFunctions.nwnxPushInt(nOverride);
            Internal.NativeFunctions.nwnxPushObject(oCreature);

            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static int GetCriticalRangeOverride(uint oCreature, int nHand = 0, BaseItem baseItemType = BaseItem.Invalid)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetCriticalRangeOverride");

            Internal.NativeFunctions.nwnxPushInt((int)baseItemType);
            Internal.NativeFunctions.nwnxPushInt(nHand);
            Internal.NativeFunctions.nwnxPushObject(oCreature);
            Internal.NativeFunctions.nwnxCallFunction();

            return Internal.NativeFunctions.nwnxPopInt();
        }

        public static void AddAssociate(uint oCreature, uint oAssociate, int nAssociateType)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "AddAssociate");

            Internal.NativeFunctions.nwnxPushInt(nAssociateType);
            Internal.NativeFunctions.nwnxPushObject(oAssociate);
            Internal.NativeFunctions.nwnxPushObject(oCreature);

            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static int GetWalkAnimation(uint oCreature)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetWalkAnimation");

            Internal.NativeFunctions.nwnxPushObject(oCreature);
            Internal.NativeFunctions.nwnxCallFunction();

            return Internal.NativeFunctions.nwnxPopInt();
        }

        public static void SetWalkAnimation(uint oCreature, int nAnimation)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetWalkAnimation");
            Internal.NativeFunctions.nwnxPushInt(nAnimation);
            Internal.NativeFunctions.nwnxPushObject(oCreature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static void SetAttackRollOverride(uint oCreature, int nRoll, int nModifier)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetAttackRollOverride");
            Internal.NativeFunctions.nwnxPushInt(nModifier);
            Internal.NativeFunctions.nwnxPushInt(nRoll);
            Internal.NativeFunctions.nwnxPushObject(oCreature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static void SetParryAllAttacks(uint oCreature, bool bParry)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetParryAllAttacks");
            Internal.NativeFunctions.nwnxPushInt(bParry ? 1 : 0);
            Internal.NativeFunctions.nwnxPushObject(oCreature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static bool GetNoPermanentDeath(uint oCreature)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetNoPermanentDeath");

            Internal.NativeFunctions.nwnxPushObject(oCreature);
            Internal.NativeFunctions.nwnxCallFunction();

            return Internal.NativeFunctions.nwnxPopInt() == 1;
        }

        public static void SetNoPermanentDeath(uint oCreature, bool bNoPermanentDeath)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetNoPermanentDeath");
            Internal.NativeFunctions.nwnxPushInt(bNoPermanentDeath ? 1 : 0);
            Internal.NativeFunctions.nwnxPushObject(oCreature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static Vector3 ComputeSafeLocation(uint oCreature, Vector3 vPosition, float fRadius = 20.0f, bool bWalkStraightLineRequired = true)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ComputeSafeLocation"); 
            
            Internal.NativeFunctions.nwnxPushInt(bWalkStraightLineRequired ? 1 : 0);
            Internal.NativeFunctions.nwnxPushFloat(fRadius);
            Internal.NativeFunctions.nwnxPushFloat(vPosition.X);
            Internal.NativeFunctions.nwnxPushFloat(vPosition.Y);
            Internal.NativeFunctions.nwnxPushFloat(vPosition.Z);
            Internal.NativeFunctions.nwnxPushObject(oCreature);
            Internal.NativeFunctions.nwnxCallFunction();

            return new Vector3
            {
                Z = Internal.NativeFunctions.nwnxPopFloat(),
                Y = Internal.NativeFunctions.nwnxPopFloat(),
                X = Internal.NativeFunctions.nwnxPopFloat()
            };
        }

        public static void DoPerceptionUpdateOnCreature(uint oCreature, uint oTargetCreature)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "DoPerceptionUpdateOnCreature");
            Internal.NativeFunctions.nwnxPushObject(oTargetCreature);
            Internal.NativeFunctions.nwnxPushObject(oCreature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static float GetPersonalSpace(uint oCreature)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetPersonalSpace");

            Internal.NativeFunctions.nwnxPushObject(oCreature);
            Internal.NativeFunctions.nwnxCallFunction();

            return Internal.NativeFunctions.nwnxPopFloat();
        }

        public static void SetPersonalSpace(uint oCreature, float fPerspace)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetPersonalSpace");
            Internal.NativeFunctions.nwnxPushFloat(fPerspace);
            Internal.NativeFunctions.nwnxPushObject(oCreature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static float GetCreaturePersonalSpace(uint oCreature)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetCreaturePersonalSpace");

            Internal.NativeFunctions.nwnxPushObject(oCreature);
            Internal.NativeFunctions.nwnxCallFunction();

            return Internal.NativeFunctions.nwnxPopFloat();
        }

        public static void SetCreaturePersonalSpace(uint oCreature, float fCrePerspace)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetCreaturePersonalSpace");
            Internal.NativeFunctions.nwnxPushFloat(fCrePerspace);
            Internal.NativeFunctions.nwnxPushObject(oCreature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static float GetHeight(uint oCreature)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetHeight");

            Internal.NativeFunctions.nwnxPushObject(oCreature);
            Internal.NativeFunctions.nwnxCallFunction();

            return Internal.NativeFunctions.nwnxPopFloat();
        }

        public static void SetHeight(uint oCreature, float fHeight)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetHeight");
            Internal.NativeFunctions.nwnxPushFloat(fHeight);
            Internal.NativeFunctions.nwnxPushObject(oCreature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static float GetHitDistance(uint oCreature)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetHitDistance");

            Internal.NativeFunctions.nwnxPushObject(oCreature);
            Internal.NativeFunctions.nwnxCallFunction();

            return Internal.NativeFunctions.nwnxPopFloat();
        }

        public static void SetHitDistance(uint oCreature, float fHitDist)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetHitDistance");
            Internal.NativeFunctions.nwnxPushFloat(fHitDist);
            Internal.NativeFunctions.nwnxPushObject(oCreature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static float GetPreferredAttackDistance(uint oCreature)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetPreferredAttackDistance");

            Internal.NativeFunctions.nwnxPushObject(oCreature);
            Internal.NativeFunctions.nwnxCallFunction();

            return Internal.NativeFunctions.nwnxPopFloat();
        }

        public static void SetPreferredAttackDistance(uint oCreature, float fPrefAtckDist)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetPreferredAttackDistance");
            Internal.NativeFunctions.nwnxPushFloat(fPrefAtckDist);
            Internal.NativeFunctions.nwnxPushObject(oCreature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static int GetArmorCheckPenalty(uint oCreature)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetArmorCheckPenalty");

            Internal.NativeFunctions.nwnxPushObject(oCreature);
            Internal.NativeFunctions.nwnxCallFunction();

            return Internal.NativeFunctions.nwnxPopInt();
        }

        public static int GetShieldCheckPenalty(uint oCreature)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetShieldCheckPenalty");

            Internal.NativeFunctions.nwnxPushObject(oCreature);
            Internal.NativeFunctions.nwnxCallFunction();

            return Internal.NativeFunctions.nwnxPopInt();
        }

        public static void SetBypassEffectImmunity(uint oCreature, int nImmunityType, int nChance = 100, bool bPersist = false)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetBypassEffectImmunity");
            Internal.NativeFunctions.nwnxPushInt(bPersist ? 1 : 0);
            Internal.NativeFunctions.nwnxPushInt(nChance);
            Internal.NativeFunctions.nwnxPushInt(nImmunityType);
            Internal.NativeFunctions.nwnxPushObject(oCreature);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static int GetBypassEffectImmunity(uint oCreature, int nImmunityType)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetBypassEffectImmunity");

            Internal.NativeFunctions.nwnxPushInt(nImmunityType);
            Internal.NativeFunctions.nwnxPushObject(oCreature);
            Internal.NativeFunctions.nwnxCallFunction();

            return Internal.NativeFunctions.nwnxPopInt();
        }
    }
}