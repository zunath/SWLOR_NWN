using System.Numerics;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Creature;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Core.NWNX
{
    public static class CreaturePlugin
    {
        private const string PLUGIN_NAME = "NWNX_Creature";

        // Gives the provided creature the provided feat.
        public static void AddFeat(uint creature, FeatType feat)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "AddFeat");
            NWNCore.NativeFunctions.nwnxPushInt((int)feat);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Gives the provided creature the provided feat.
        // Adds the feat to the stat list at the provided level.
        public static void AddFeatByLevel(uint creature, FeatType feat, int level)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "AddFeatByLevel");
            NWNCore.NativeFunctions.nwnxPushInt(level);
            NWNCore.NativeFunctions.nwnxPushInt((int)feat);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Removes from the provided creature the provided feat.
        public static void RemoveFeat(uint creature, FeatType feat)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "RemoveFeat");
            NWNCore.NativeFunctions.nwnxPushInt((int)feat);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static bool GetKnowsFeat(uint creature, FeatType feat)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetKnowsFeat");
            NWNCore.NativeFunctions.nwnxPushInt((int)feat);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt() != 0;
        }

        // Returns the count of feats learned at the provided level.
        public static int GetFeatCountByLevel(uint creature, int level)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetFeatCountByLevel");
            NWNCore.NativeFunctions.nwnxPushInt(level);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Returns the feat learned at the provided level at the provided index.
        // Index bounds: 0 <= index < GetFeatCountByLevel(creature, level).
        public static FeatType GetFeatByLevel(uint creature, int level, int index)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetFeatByLevel");
            NWNCore.NativeFunctions.nwnxPushInt(index);
            NWNCore.NativeFunctions.nwnxPushInt(level);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return (FeatType)NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Returns the total number of feats known by creature
        public static int GetFeatCount(uint creature)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetFeatCount");
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Returns the creature's feat at a given index
        // Index bounds: 0 <= index < GetFeatCount(creature);
        public static FeatType GetFeatByIndex(uint creature, int index)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetFeatByIndex");
            NWNCore.NativeFunctions.nwnxPushInt(index);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return (FeatType)NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Returns TRUE if creature meets all requirements to take given feat
        public static bool GetMeetsFeatRequirements(uint creature, FeatType feat)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetMeetsFeatRequirements");
            NWNCore.NativeFunctions.nwnxPushInt((int)feat);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt() != 0;
        }

        // Returns the special ability of the provided creature at the provided index.
        // Index bounds: 0 <= index < GetSpecialAbilityCount(creature).
        public static SpecialAbilitySlot GetSpecialAbility(uint creature, int index)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetSpecialAbility");
            var ability = new SpecialAbilitySlot();
            NWNCore.NativeFunctions.nwnxPushInt(index);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
            ability.Level = NWNCore.NativeFunctions.nwnxPopInt();
            ability.Ready = NWNCore.NativeFunctions.nwnxPopInt();
            ability.ID = NWNCore.NativeFunctions.nwnxPopInt();
            return ability;
        }

        // Returns the count of special ability count of the provided creature.
        public static int GetSpecialAbilityCount(uint creature)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetSpecialAbilityCount");
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Adds the provided special ability to the provided creature.
        public static void AddSpecialAbility(uint creature, SpecialAbilitySlot ability)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "AddSpecialAbility");
            NWNCore.NativeFunctions.nwnxPushInt(ability.ID);
            NWNCore.NativeFunctions.nwnxPushInt(ability.Ready);
            NWNCore.NativeFunctions.nwnxPushInt(ability.Level);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Removes the provided special ability from the provided creature.
        // Index bounds: 0 <= index < GetSpecialAbilityCount(creature).
        public static void RemoveSpecialAbility(uint creature, int index)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "RemoveSpecialAbility");
            NWNCore.NativeFunctions.nwnxPushInt(index);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Sets the special ability at the provided index for the provided creature to the provided ability.
        // Index bounds: 0 <= index < GetSpecialAbilityCount(creature).
        public static void SetSpecialAbility(uint creature, int index, SpecialAbilitySlot ability)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetSpecialAbility");
            NWNCore.NativeFunctions.nwnxPushInt(ability.ID);
            NWNCore.NativeFunctions.nwnxPushInt(ability.Ready);
            NWNCore.NativeFunctions.nwnxPushInt(ability.Level);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Returns the classId taken by the provided creature at the provided level.
        public static ClassType GetClassByLevel(uint creature, int level)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetClassByLevel");
            NWNCore.NativeFunctions.nwnxPushInt(level);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return (ClassType)NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Sets the base AC for the provided creature.
        public static void SetBaseAC(uint creature, int ac)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetBaseAC");
            NWNCore.NativeFunctions.nwnxPushInt(ac);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Returns the base AC for the provided creature.
        public static int GetBaseAC(uint creature)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetBaseAC");
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Sets the provided ability score of provided creature to the provided value. Does not apply racial bonuses/penalties.
        public static void SetRawAbilityScore(uint creature, AbilityType ability, int value)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetRawAbilityScore");
            NWNCore.NativeFunctions.nwnxPushInt(value);
            NWNCore.NativeFunctions.nwnxPushInt((int)ability);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Gets the provided ability score of provided creature. Does not apply racial bonuses/penalties.
        public static int GetRawAbilityScore(uint creature, AbilityType ability)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetRawAbilityScore");
            NWNCore.NativeFunctions.nwnxPushInt((int)ability);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Adjusts the provided ability score of a provided creature. Does not apply racial bonuses/penalties.
        public static void ModifyRawAbilityScore(uint creature, AbilityType ability, int modifier)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ModifyRawAbilityScore");
            NWNCore.NativeFunctions.nwnxPushInt(modifier);
            NWNCore.NativeFunctions.nwnxPushInt((int)ability);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Gets the raw ability score a polymorphed creature had prior to polymorphing. Str/Dex/Con only.
        public static int GetPrePolymorphAbilityScore(uint creature, AbilityType ability)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetPrePolymorphAbilityScore");
            NWNCore.NativeFunctions.nwnxPushInt((int)ability);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Gets the remaining spell slots (innate casting) for the provided creature for the provided classId and level.
        public static int GetRemainingSpellSlots(uint creature, ClassType classId, int level)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetRemainingSpellSlots");
            NWNCore.NativeFunctions.nwnxPushInt(level);
            NWNCore.NativeFunctions.nwnxPushInt((int)classId);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Sets the remaining spell slots (innate casting) for the provided creature for the provided classId and level.
        public static void SetRemainingSpellSlots(uint creature, ClassType classId, int level, int slots)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetRemainingSpellSlots");
            NWNCore.NativeFunctions.nwnxPushInt(slots);
            NWNCore.NativeFunctions.nwnxPushInt(level);
            NWNCore.NativeFunctions.nwnxPushInt((int)classId);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Remove a spell from creature's spellbook for class.
        public static void RemoveKnownSpell(uint creature, ClassType classId, int level, int spellId)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "RemoveKnownSpell");
            NWNCore.NativeFunctions.nwnxPushInt(spellId);
            NWNCore.NativeFunctions.nwnxPushInt(level);
            NWNCore.NativeFunctions.nwnxPushInt((int)classId);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Add a new spell to creature's spellbook for class.
        public static void AddKnownSpell(uint creature, ClassType classId, int level, int spellId)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "AddKnownSpell");
            NWNCore.NativeFunctions.nwnxPushInt(spellId);
            NWNCore.NativeFunctions.nwnxPushInt(level);
            NWNCore.NativeFunctions.nwnxPushInt((int)classId);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Gets the maximum count of spell slots for the proivded creature for the provided classId and level.
        public static int GetMaxSpellSlots(uint creature, ClassType classId, int level)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetMaxSpellSlots");
            NWNCore.NativeFunctions.nwnxPushInt(level);
            NWNCore.NativeFunctions.nwnxPushInt((int)classId);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Gets the maximum hit points for creature for level.
        public static int GetMaxHitPointsByLevel(uint creature, int level)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetMaxHitPointsByLevel");
            NWNCore.NativeFunctions.nwnxPushInt(level);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Sets the maximum hit points for creature for level to nValue.
        public static void SetMaxHitPointsByLevel(uint creature, int level, int value)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetMaxHitPointsByLevel");
            NWNCore.NativeFunctions.nwnxPushInt(value);
            NWNCore.NativeFunctions.nwnxPushInt(level);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Set creature's movement rate.
        public static void SetMovementRate(uint creature, MovementRate rate)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetMovementRate");
            NWNCore.NativeFunctions.nwnxPushInt((int)rate);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Returns the creature's current movement rate factor (base = 1.0)
        public static float GetMovementRateFactor(uint creature)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetMovementRateFactor");
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopFloat();
        }

        // Sets the creature's current movement rate factor (base = 1.0)
        public static void SetMovementRateFactor(uint creature, float factor)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetMovementRateFactor");
            NWNCore.NativeFunctions.nwnxPushFloat(factor);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Set creature's raw good/evil alignment value.
        public static void SetAlignmentGoodEvil(uint creature, int value)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetAlignmentGoodEvil");
            NWNCore.NativeFunctions.nwnxPushInt(value);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Set creature's raw law/chaos alignment value.
        public static void SetAlignmentLawChaos(uint creature, int value)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetAlignmentLawChaos");
            NWNCore.NativeFunctions.nwnxPushInt(value);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Set the base ranks in a skill for creature
        public static void SetSkillRank(uint creature, NWNSkillType skill, int rank)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetSkillRank");
            NWNCore.NativeFunctions.nwnxPushInt(rank);
            NWNCore.NativeFunctions.nwnxPushInt((int)skill);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Set the classId ID in a particular position for a creature.
        // Position should be 0, 1, or 2.
        // ClassID should be a valid ID number in classes.2da and be between 0 and 255.
        public static void SetClassByPosition(uint creature, int position, ClassType classId, bool updateLevels = true)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetClassByPosition");
            NWNCore.NativeFunctions.nwnxPushInt(updateLevels ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushInt((int)classId);
            NWNCore.NativeFunctions.nwnxPushInt(position);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Set creature's base attack bonus (BAB)
        // Modifying the BAB will also affect the creature's attacks per round and its
        // eligibility for feats, prestige classes, etc.
        // The BAB value should be between 0 and 254.
        // Setting BAB to 0 will cause the creature to revert to its original BAB based
        // on its classes and levels. A creature can never have an actual BAB of zero.
        // NOTE: The base game has a function SetBaseAttackBonus(), which actually sets
        //       the bonus attacks per round for a creature, not the BAB.
        public static void SetBaseAttackBonus(uint creature, int bab)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetBaseAttackBonus");
            NWNCore.NativeFunctions.nwnxPushInt(bab);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Gets the creatures current attacks per round (using equipped weapon)
        // bBaseAPR - If true, will return the base attacks per round, based on BAB and
        //            equipped weapons, regardless of overrides set by
        //            calls to SetBaseAttackBonus() builtin function.
        public static int GetAttacksPerRound(uint creature, bool bBaseAPR)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetAttacksPerRound");
            NWNCore.NativeFunctions.nwnxPushInt(bBaseAPR ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Restore all creature feat uses
        public static void RestoreFeats(uint creature)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "RestoreFeats");
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Restore all creature special ability uses
        public static void RestoreSpecialAbilities(uint creature)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "RestoreSpecialAbilities");
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Restore uses for all items carried by the creature
        public static void RestoreItems(uint creature)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "RestoreItems");
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Sets the creature size. Use CREATURE_SIZE_* constants
        public static void SetSize(uint creature, CreatureSize creatureSize)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetSize");
            NWNCore.NativeFunctions.nwnxPushInt((int)creatureSize);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Gets the creature's remaining unspent skill points
        public static int GetSkillPointsRemaining(uint creature)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetSkillPointsRemaining");
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }


        // Sets the creature's remaining unspent skill points
        public static void SetSkillPointsRemaining(uint creature, int skillpoints)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetSkillPointsRemaining");
            NWNCore.NativeFunctions.nwnxPushInt(skillpoints);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Sets the creature's racial type
        public static void SetRacialType(uint creature, RacialType racialtype)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetRacialType");
            NWNCore.NativeFunctions.nwnxPushInt((int)racialtype);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Returns the creature's current movement type (MOVEMENT_TYPE_*)
        public static MovementType GetMovementType(uint creature)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetMovementType");
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return (MovementType)NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Sets the maximum movement rate a creature can have while walking (not running)
        // This allows a creature with movement speed enhancemens to walk at a normal rate.
        // Setting the value to -1.0 will remove the cap.
        // Default value is 2000.0, which is the base human walk speed.
        public static void SetWalkRateCap(uint creature, float fWalkRate)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetWalkRateCap");
            NWNCore.NativeFunctions.nwnxPushFloat(fWalkRate);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Sets the creature's gold without sending a feedback message
        public static void SetGold(uint creature, int gold)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetGold");
            NWNCore.NativeFunctions.nwnxPushInt(gold);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Sets corpse decay time in milliseconds
        public static void SetCorpseDecayTime(uint creature, int decayTimeMs)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetCorpseDecayTime");
            NWNCore.NativeFunctions.nwnxPushInt(decayTimeMs);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Returns the creature's base save and any modifiers set in the toolset
        public static int GetBaseSavingThrow(uint creature, int which)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetBaseSavingThrow");
            NWNCore.NativeFunctions.nwnxPushInt(which);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Sets the base saving throw of the creature
        public static void SetBaseSavingThrow(uint creature, SavingThrow which, int value)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetBaseSavingThrow");
            NWNCore.NativeFunctions.nwnxPushInt(value);
            NWNCore.NativeFunctions.nwnxPushInt((int)which);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Add count levels of class to the creature, bypassing all validation
        // This will not work on player characters
        public static void LevelUp(uint creature, ClassType classId, int count = 1)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "LevelUp");
            NWNCore.NativeFunctions.nwnxPushInt(count);
            NWNCore.NativeFunctions.nwnxPushInt((int)classId);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Remove last count levels from a creature
        // This will not work on player characters
        public static void LevelDown(uint creature, int count = 1)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "LevelDown");
            NWNCore.NativeFunctions.nwnxPushInt(count);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Sets the creature's challenge rating
        public static void SetChallengeRating(uint creature, float fCR)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetChallengeRating");
            NWNCore.NativeFunctions.nwnxPushFloat(fCR);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
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
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetAttackBonus");
            NWNCore.NativeFunctions.nwnxPushInt(includeBaseAttackBonus ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushInt(isOffhand ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushInt(isTouchAttack ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushInt(isMelee ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Get highest level version of feat posessed by creature (e.g. for barbarian rage)
        public static int GetHighestLevelOfFeat(uint creature, int feat)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetHighestLevelOfFeat");
            NWNCore.NativeFunctions.nwnxPushInt(feat);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Get feat remaining uses of a creature
        public static int GetFeatRemainingUses(uint creature, FeatType feat)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetFeatRemainingUses");
            NWNCore.NativeFunctions.nwnxPushInt((int)feat);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Get feat total uses of a creature
        public static int GetFeatTotalUses(uint creature, FeatType feat)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetFeatTotalUses");
            NWNCore.NativeFunctions.nwnxPushInt((int)feat);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Set feat remaining uses of a creature
        public static void SetFeatRemainingUses(uint creature, FeatType feat, int uses)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetFeatRemainingUses");
            NWNCore.NativeFunctions.nwnxPushInt(uses);
            NWNCore.NativeFunctions.nwnxPushInt((int)feat);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Get total effect bonus
        public static int GetTotalEffectBonus(uint creature, BonusType bonusType = BonusType.Attack,
            uint target = NWScript.NWScript.OBJECT_INVALID, bool isElemental = false,
            bool isForceMax = false, int saveType = -1, int saveSpecificType = -1, NWNSkillType skill = NWNSkillType.Invalid,
            int abilityScore = -1, bool isOffhand = false)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetTotalEffectBonus");
            NWNCore.NativeFunctions.nwnxPushInt(isOffhand ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushInt(abilityScore);
            NWNCore.NativeFunctions.nwnxPushInt((int)skill);
            NWNCore.NativeFunctions.nwnxPushInt(saveSpecificType);
            NWNCore.NativeFunctions.nwnxPushInt(saveType);
            NWNCore.NativeFunctions.nwnxPushInt(isForceMax ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushInt(isElemental ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushObject(target);
            NWNCore.NativeFunctions.nwnxPushInt((int)bonusType);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Set the original first or last name of creature
        //
        // For PCs this will persist to the .bic file if saved. Requires a relog to update.
        public static void SetOriginalName(uint creature, string name, bool isLastName)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetOriginalName");
            NWNCore.NativeFunctions.nwnxPushInt(isLastName ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushString(name);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Get the original first or last name of creature
        public static string GetOriginalName(uint creature, bool isLastName)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetOriginalName");
            NWNCore.NativeFunctions.nwnxPushInt(isLastName ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopString();
        }

        // Set creature's spell resistance
        public static void SetSpellResistance(uint creature, int sr)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetSpellResistance");
            NWNCore.NativeFunctions.nwnxPushInt(sr);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Set creature's animal companion's name
        /// @param creature The master creature object.
        /// @param name The name to give their animal companion.
        public static void SetAnimalCompanionName(uint creature, string name)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetAnimalCompanionCreatureType");
            NWNCore.NativeFunctions.nwnxPushString(name);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Set creature's familiar's name
        /// @param creature The master creature object.
        /// @param name The name to give their familiar.
        public static void SetFamiliarName(uint creature, string name)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetFamiliarCreatureType");
            NWNCore.NativeFunctions.nwnxPushString(name);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Get whether the creature can be disarmed.
        /// @param creature The creature object.
        /// @return TRUE if the creature can be disarmed.
        public static int GetDisarmable(uint creature)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetDisarmable");
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        /// @brief Set whether a creature can be disarmed.
        /// @param creature The creature object.
        /// @param disarmable Set to TRUE if the creature can be disarmed.
        public static void SetDisarmable(uint creature, bool isDisarmable)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetDisarmable");
            NWNCore.NativeFunctions.nwnxPushInt(isDisarmable ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Sets one of creature's domains.
        /// @param creature The creature object.
        /// @param class The class id from classes.2da. (Not class index 0-2)
        /// @param index The first or second domain.
        /// @param domain The domain constant to set.
        public static void SetDomain(uint creature, ClassType @class, int index, int domain)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetDomain");
            NWNCore.NativeFunctions.nwnxPushInt(domain);
            NWNCore.NativeFunctions.nwnxPushInt(index);
            NWNCore.NativeFunctions.nwnxPushInt((int)@class);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Sets creature's specialist school.
        /// @param creature The creature object.
        /// @param class The class id from classes.2da. (Not class index 0-2)
        /// @param school The school constant.
        public static void SetSpecialization(uint creature, ClassType @class, int school)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetSpecialization");
            NWNCore.NativeFunctions.nwnxPushInt(school);
            NWNCore.NativeFunctions.nwnxPushInt((int)@class);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Sets oCreatures faction to be the faction with id nFactionId.
        /// @param oCreature The creature.
        /// @param nFactionId The faction id we want the creature to join.
        public static void SetFaction(uint creature, int factionId)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetFaction");
            NWNCore.NativeFunctions.nwnxPushInt(factionId);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Gets the faction id from oCreature
        /// @param oCreature the creature we wish to query against
        /// @return faction id as an integer, -1 when used against invalid creature or invalid object.
        public static int GetFaction(uint creature)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetFaction");
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        /// @brief Get whether a creature is flat-footed.
        /// @param The creature object.
        /// @return TRUE if the creature is flat-footed.
        public static bool GetFlatFooted(uint creature)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetFlatFooted");
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt() == 1;
        }

        /// @brief Serialize oCreature's quickbar to a base64 string
        /// @param oCreature The creature.
        /// @return A base64 string representation of oCreature's quickbar.
        public static string SerializeQuickbar(uint creature)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SerializeQuickbar");
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopString();
        }

        /// @brief Deserialize sSerializedQuickbar for oCreature
        /// @param oCreature The creature.
        /// @param sSerializedQuickbar A base64 string of a quickbar
        /// @return TRUE on success
        public static bool DeserializeQuickbar(uint creature, string serializedQuickbar)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "DeserializeQuickbar");
            NWNCore.NativeFunctions.nwnxPushString(serializedQuickbar);
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt() == 1;
        }


        /// @brief Set the encounter source of oCreature.
        /// @param oCreature The target creature.
        /// @param oEncounter The source encounter
        public static void SetEncounter(uint oCreature, uint oEncounter)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetEncounter");

            NWNCore.NativeFunctions.nwnxPushObject(oEncounter);
            NWNCore.NativeFunctions.nwnxPushObject(oCreature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Get the encounter source of oCreature.
        /// @param oCreature The target creature.
        /// @return The encounter, OBJECT_INVALID if not part of an encounter or on error
        public static uint GetEncounter(uint oCreature)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetEncounter");

            NWNCore.NativeFunctions.nwnxPushObject(oCreature);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopObject();
        }


        /// @brief Override the damage level of oCreature.
        /// @note Damage levels are the damage state under a creature's name, for example: 'Near Death'
        /// @param oCreature The target creature.
        /// @param nDamageLevel A damage level, see damagelevels.2da. Allowed values: 0-255 or -1 to remove the override.
        public static void OverrideDamageLevel(uint oCreature, int nDamageLevel)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "OverrideDamageLevel");

            NWNCore.NativeFunctions.nwnxPushInt(nDamageLevel);
            NWNCore.NativeFunctions.nwnxPushObject(oCreature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }


        /// @brief Get if oCreature is currently bartering.
        /// @param oCreature The target creature.
        /// @return TRUE if oCreature is bartering, FALSE if not or on error.
        public static bool GetIsBartering(uint oCreature)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetIsBartering");

            NWNCore.NativeFunctions.nwnxPushObject(oCreature);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopInt() == 1;
        }


        /// @brief Sets caster level for the last item used. Use in a spellhook or spell event before to set caster level for any spells cast from the item.
        /// @param oCreature the creature who used the item.
        /// @param nCasterLvl the desired caster level.
        public static void SetLastItemCasterLevel(uint oCreature, int nCasterLvl)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetLastItemCasterLevel");

            NWNCore.NativeFunctions.nwnxPushInt(nCasterLvl);
            NWNCore.NativeFunctions.nwnxPushObject(oCreature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Gets the caster level of the last item used.
        /// @param oCreature the creature who used the item.
        /// @return returns the creatures last used item's level.
        public static int GetLastItemCasterLevel(uint oCreature)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetLastItemCasterLevel");

            NWNCore.NativeFunctions.nwnxPushObject(oCreature);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        /// @brief Gets the Armor classed of attacked against versus
        /// @param oAttacked The one being attacked
        /// @param oVersus The one doing the attacking
        /// @param nTouch TRUE for touch attacks
        /// @return -255 on Error, Flat footed AC if oVersus is invalid or the Attacked AC versus oVersus.
        public static int GetArmorClassVersus(uint oAttacked, uint oVersus, bool nTouch = false)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetArmorClassVersus");

            NWNCore.NativeFunctions.nwnxPushInt(nTouch ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushObject(oVersus);
            NWNCore.NativeFunctions.nwnxPushObject(oAttacked);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        /// @brief Move a creature to limbo.
        /// @param oCreature The creature object.
        public static void JumpToLimbo(uint oCreature)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "JumpToLimbo");
            NWNCore.NativeFunctions.nwnxPushObject(oCreature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Sets the critical hit multiplier modifier for the creature
        /// @param oCreature The target creature
        /// @param nModifier The modifier to apply
        /// @param nHand 0 for all attacks, 1 for Mainhand, 2 for Offhand
        /// @param bPersist Whether the modifier should persist to .bic file if applicable
        /// @note Persistence is activated each server reset by first use of either 'SetCriticalMultiplier*' functions. Recommended to trigger on a dummy target OnModuleLoad to enable persistence.
        public static void SetCriticalMultiplierModifier(uint oCreature, int nModifier, int nHand = 0, bool bPersist = false, BaseItem baseItemType = BaseItem.Invalid)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetCriticalMultiplierModifier");

            NWNCore.NativeFunctions.nwnxPushInt((int)baseItemType);
            NWNCore.NativeFunctions.nwnxPushInt(bPersist ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushInt(nHand);
            NWNCore.NativeFunctions.nwnxPushInt(nModifier);
            NWNCore.NativeFunctions.nwnxPushObject(oCreature);

            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Gets the critical hit multiplier modifier for the Creature
        /// @param oCreature The target creature
        /// @param nHand 0 for all attacks, 1 for Mainhand, 2 for Offhand
        /// @return the current critical hit multiplier modifier for the creature
        public static int GetCriticalMultiplierModifier(uint oCreature, int nHand = 0, BaseItem baseItemType = BaseItem.Invalid)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetCriticalMultiplierModifier");

            NWNCore.NativeFunctions.nwnxPushInt((int)baseItemType);
            NWNCore.NativeFunctions.nwnxPushInt(nHand);
            NWNCore.NativeFunctions.nwnxPushObject(oCreature);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        public static void SetCriticalMultiplierOverride(uint oCreature, int nOverride, int nHand = 0, bool bPersist = false, BaseItem baseItemType = BaseItem.Invalid)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetCriticalMultiplierOverride");

            NWNCore.NativeFunctions.nwnxPushInt((int)baseItemType);
            NWNCore.NativeFunctions.nwnxPushInt(bPersist ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushInt(nHand);
            NWNCore.NativeFunctions.nwnxPushInt(nOverride);
            NWNCore.NativeFunctions.nwnxPushObject(oCreature);

            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static int GetCriticalMultiplierOverride(uint oCreature, int nHand = 0, BaseItem baseItemType = BaseItem.Invalid)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetCriticalMultiplierOverride");

            NWNCore.NativeFunctions.nwnxPushInt((int)baseItemType);
            NWNCore.NativeFunctions.nwnxPushInt(nHand);
            NWNCore.NativeFunctions.nwnxPushObject(oCreature);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        public static void SetCriticalRangeModifier(uint oCreature, int nModifier, int nHand = 0, bool bPersist = false, BaseItem baseItemType  = BaseItem.Invalid)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetCriticalRangeModifier");

            NWNCore.NativeFunctions.nwnxPushInt((int)baseItemType);
            NWNCore.NativeFunctions.nwnxPushInt(bPersist ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushInt(nHand);
            NWNCore.NativeFunctions.nwnxPushInt(nModifier);
            NWNCore.NativeFunctions.nwnxPushObject(oCreature);

            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static int GetCriticalRangeModifier(uint oCreature, int nHand = 0, BaseItem baseItemType = BaseItem.Invalid)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetCriticalRangeModifier");

            NWNCore.NativeFunctions.nwnxPushInt((int)baseItemType);
            NWNCore.NativeFunctions.nwnxPushInt(nHand);
            NWNCore.NativeFunctions.nwnxPushObject(oCreature);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        public static void SetCriticalRangeOverride(uint oCreature, int nOverride, int nHand = 0, bool bPersist = false, BaseItem baseItemType = BaseItem.Invalid)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetCriticalRangeOverride");

            NWNCore.NativeFunctions.nwnxPushInt((int)baseItemType);
            NWNCore.NativeFunctions.nwnxPushInt(bPersist ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushInt(nHand);
            NWNCore.NativeFunctions.nwnxPushInt(nOverride);
            NWNCore.NativeFunctions.nwnxPushObject(oCreature);

            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static int GetCriticalRangeOverride(uint oCreature, int nHand = 0, BaseItem baseItemType = BaseItem.Invalid)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetCriticalRangeOverride");

            NWNCore.NativeFunctions.nwnxPushInt((int)baseItemType);
            NWNCore.NativeFunctions.nwnxPushInt(nHand);
            NWNCore.NativeFunctions.nwnxPushObject(oCreature);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        public static void AddAssociate(uint oCreature, uint oAssociate, int nAssociateType)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "AddAssociate");

            NWNCore.NativeFunctions.nwnxPushInt(nAssociateType);
            NWNCore.NativeFunctions.nwnxPushObject(oAssociate);
            NWNCore.NativeFunctions.nwnxPushObject(oCreature);

            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static int GetWalkAnimation(uint oCreature)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetWalkAnimation");

            NWNCore.NativeFunctions.nwnxPushObject(oCreature);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        public static void SetWalkAnimation(uint oCreature, int nAnimation)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetWalkAnimation");
            NWNCore.NativeFunctions.nwnxPushInt(nAnimation);
            NWNCore.NativeFunctions.nwnxPushObject(oCreature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static void SetAttackRollOverride(uint oCreature, int nRoll, int nModifier)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetAttackRollOverride");
            NWNCore.NativeFunctions.nwnxPushInt(nModifier);
            NWNCore.NativeFunctions.nwnxPushInt(nRoll);
            NWNCore.NativeFunctions.nwnxPushObject(oCreature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static void SetParryAllAttacks(uint oCreature, bool bParry)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetParryAllAttacks");
            NWNCore.NativeFunctions.nwnxPushInt(bParry ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushObject(oCreature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static bool GetNoPermanentDeath(uint oCreature)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetNoPermanentDeath");

            NWNCore.NativeFunctions.nwnxPushObject(oCreature);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopInt() == 1;
        }

        public static void SetNoPermanentDeath(uint oCreature, bool bNoPermanentDeath)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetNoPermanentDeath");
            NWNCore.NativeFunctions.nwnxPushInt(bNoPermanentDeath ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushObject(oCreature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static Vector3 ComputeSafeLocation(uint oCreature, Vector3 vPosition, float fRadius = 20.0f, bool bWalkStraightLineRequired = true)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ComputeSafeLocation"); 
            
            NWNCore.NativeFunctions.nwnxPushInt(bWalkStraightLineRequired ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushFloat(fRadius);
            NWNCore.NativeFunctions.nwnxPushFloat(vPosition.X);
            NWNCore.NativeFunctions.nwnxPushFloat(vPosition.Y);
            NWNCore.NativeFunctions.nwnxPushFloat(vPosition.Z);
            NWNCore.NativeFunctions.nwnxPushObject(oCreature);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return new Vector3
            {
                Z = NWNCore.NativeFunctions.nwnxPopFloat(),
                Y = NWNCore.NativeFunctions.nwnxPopFloat(),
                X = NWNCore.NativeFunctions.nwnxPopFloat()
            };
        }

        public static void DoPerceptionUpdateOnCreature(uint oCreature, uint oTargetCreature)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "DoPerceptionUpdateOnCreature");
            NWNCore.NativeFunctions.nwnxPushObject(oTargetCreature);
            NWNCore.NativeFunctions.nwnxPushObject(oCreature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static float GetPersonalSpace(uint oCreature)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetPersonalSpace");

            NWNCore.NativeFunctions.nwnxPushObject(oCreature);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopFloat();
        }

        public static void SetPersonalSpace(uint oCreature, float fPerspace)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetPersonalSpace");
            NWNCore.NativeFunctions.nwnxPushFloat(fPerspace);
            NWNCore.NativeFunctions.nwnxPushObject(oCreature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static float GetCreaturePersonalSpace(uint oCreature)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetCreaturePersonalSpace");

            NWNCore.NativeFunctions.nwnxPushObject(oCreature);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopFloat();
        }

        public static void SetCreaturePersonalSpace(uint oCreature, float fCrePerspace)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetCreaturePersonalSpace");
            NWNCore.NativeFunctions.nwnxPushFloat(fCrePerspace);
            NWNCore.NativeFunctions.nwnxPushObject(oCreature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static float GetHeight(uint oCreature)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetHeight");

            NWNCore.NativeFunctions.nwnxPushObject(oCreature);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopFloat();
        }

        public static void SetHeight(uint oCreature, float fHeight)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetHeight");
            NWNCore.NativeFunctions.nwnxPushFloat(fHeight);
            NWNCore.NativeFunctions.nwnxPushObject(oCreature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static float GetHitDistance(uint oCreature)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetHitDistance");

            NWNCore.NativeFunctions.nwnxPushObject(oCreature);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopFloat();
        }

        public static void SetHitDistance(uint oCreature, float fHitDist)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetHitDistance");
            NWNCore.NativeFunctions.nwnxPushFloat(fHitDist);
            NWNCore.NativeFunctions.nwnxPushObject(oCreature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static float GetPreferredAttackDistance(uint oCreature)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetPreferredAttackDistance");

            NWNCore.NativeFunctions.nwnxPushObject(oCreature);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopFloat();
        }

        public static void SetPreferredAttackDistance(uint oCreature, float fPrefAtckDist)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetPreferredAttackDistance");
            NWNCore.NativeFunctions.nwnxPushFloat(fPrefAtckDist);
            NWNCore.NativeFunctions.nwnxPushObject(oCreature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static int GetArmorCheckPenalty(uint oCreature)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetArmorCheckPenalty");

            NWNCore.NativeFunctions.nwnxPushObject(oCreature);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        public static int GetShieldCheckPenalty(uint oCreature)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetShieldCheckPenalty");

            NWNCore.NativeFunctions.nwnxPushObject(oCreature);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        public static void SetBypassEffectImmunity(uint oCreature, int nImmunityType, int nChance = 100, bool bPersist = false)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetBypassEffectImmunity");
            NWNCore.NativeFunctions.nwnxPushInt(bPersist ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushInt(nChance);
            NWNCore.NativeFunctions.nwnxPushInt(nImmunityType);
            NWNCore.NativeFunctions.nwnxPushObject(oCreature);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static int GetBypassEffectImmunity(uint oCreature, int nImmunityType)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetBypassEffectImmunity");

            NWNCore.NativeFunctions.nwnxPushInt(nImmunityType);
            NWNCore.NativeFunctions.nwnxPushObject(oCreature);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopInt();
        }
    }
}