using System.Numerics;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Core.NWNX
{
    public static class CreaturePlugin
    {
        private const string PLUGIN_NAME = "NWNX_Creature";

        // Gives the provided creature the provided feat.
        public static void AddFeat(uint creature, FeatType feat)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "AddFeat");
            NWNXPInvoke.NWNXPushInt((int)feat);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Gives the provided creature the provided feat.
        // Adds the feat to the stat list at the provided level.
        public static void AddFeatByLevel(uint creature, FeatType feat, int level)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "AddFeatByLevel");
            NWNXPInvoke.NWNXPushInt(level);
            NWNXPInvoke.NWNXPushInt((int)feat);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Removes from the provided creature the provided feat.
        public static void RemoveFeat(uint creature, FeatType feat)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "RemoveFeat");
            NWNXPInvoke.NWNXPushInt((int)feat);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static bool GetKnowsFeat(uint creature, FeatType feat)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetKnowsFeat");
            NWNXPInvoke.NWNXPushInt((int)feat);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt() != 0;
        }

        // Returns the count of feats learned at the provided level.
        public static int GetFeatCountByLevel(uint creature, int level)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetFeatCountByLevel");
            NWNXPInvoke.NWNXPushInt(level);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt();
        }

        // Returns the feat learned at the provided level at the provided index.
        // Index bounds: 0 <= index < GetFeatCountByLevel(creature, level).
        public static FeatType GetFeatByLevel(uint creature, int level, int index)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetFeatByLevel");
            NWNXPInvoke.NWNXPushInt(index);
            NWNXPInvoke.NWNXPushInt(level);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
            return (FeatType)NWNXPInvoke.NWNXPopInt();
        }

        // Returns the total number of feats known by creature
        public static int GetFeatCount(uint creature)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetFeatCount");
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt();
        }

        // Returns the creature's feat at a given index
        // Index bounds: 0 <= index < GetFeatCount(creature);
        public static FeatType GetFeatByIndex(uint creature, int index)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetFeatByIndex");
            NWNXPInvoke.NWNXPushInt(index);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
            return (FeatType)NWNXPInvoke.NWNXPopInt();
        }

        // Returns TRUE if creature meets all requirements to take given feat
        public static bool GetMeetsFeatRequirements(uint creature, FeatType feat)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetMeetsFeatRequirements");
            NWNXPInvoke.NWNXPushInt((int)feat);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt() != 0;
        }

        // Returns the special ability of the provided creature at the provided index.
        // Index bounds: 0 <= index < GetSpecialAbilityCount(creature).
        public static SpecialAbilitySlot GetSpecialAbility(uint creature, int index)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetSpecialAbility");
            var ability = new SpecialAbilitySlot();
            NWNXPInvoke.NWNXPushInt(index);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
            ability.Level = NWNXPInvoke.NWNXPopInt();
            ability.Ready = NWNXPInvoke.NWNXPopInt();
            ability.ID = NWNXPInvoke.NWNXPopInt();
            return ability;
        }

        // Returns the count of special ability count of the provided creature.
        public static int GetSpecialAbilityCount(uint creature)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetSpecialAbilityCount");
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt();
        }

        // Adds the provided special ability to the provided creature.
        public static void AddSpecialAbility(uint creature, SpecialAbilitySlot ability)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "AddSpecialAbility");
            NWNXPInvoke.NWNXPushInt(ability.ID);
            NWNXPInvoke.NWNXPushInt(ability.Ready);
            NWNXPInvoke.NWNXPushInt(ability.Level);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Removes the provided special ability from the provided creature.
        // Index bounds: 0 <= index < GetSpecialAbilityCount(creature).
        public static void RemoveSpecialAbility(uint creature, int index)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "RemoveSpecialAbility");
            NWNXPInvoke.NWNXPushInt(index);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Sets the special ability at the provided index for the provided creature to the provided ability.
        // Index bounds: 0 <= index < GetSpecialAbilityCount(creature).
        public static void SetSpecialAbility(uint creature, int index, SpecialAbilitySlot ability)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetSpecialAbility");
            NWNXPInvoke.NWNXPushInt(ability.ID);
            NWNXPInvoke.NWNXPushInt(ability.Ready);
            NWNXPInvoke.NWNXPushInt(ability.Level);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Returns the classId taken by the provided creature at the provided level.
        public static ClassType GetClassByLevel(uint creature, int level)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetClassByLevel");
            NWNXPInvoke.NWNXPushInt(level);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
            return (ClassType)NWNXPInvoke.NWNXPopInt();
        }

        // Sets the base AC for the provided creature.
        public static void SetBaseAC(uint creature, int ac)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetBaseAC");
            NWNXPInvoke.NWNXPushInt(ac);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Returns the base AC for the provided creature.
        public static int GetBaseAC(uint creature)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetBaseAC");
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt();
        }

        // Sets the provided ability score of provided creature to the provided value. Does not apply racial bonuses/penalties.
        public static void SetRawAbilityScore(uint creature, AbilityType ability, int value)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetRawAbilityScore");
            NWNXPInvoke.NWNXPushInt(value);
            NWNXPInvoke.NWNXPushInt((int)ability);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Gets the provided ability score of provided creature. Does not apply racial bonuses/penalties.
        public static int GetRawAbilityScore(uint creature, AbilityType ability)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetRawAbilityScore");
            NWNXPInvoke.NWNXPushInt((int)ability);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt();
        }

        // Adjusts the provided ability score of a provided creature. Does not apply racial bonuses/penalties.
        public static void ModifyRawAbilityScore(uint creature, AbilityType ability, int modifier)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "ModifyRawAbilityScore");
            NWNXPInvoke.NWNXPushInt(modifier);
            NWNXPInvoke.NWNXPushInt((int)ability);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Gets the raw ability score a polymorphed creature had prior to polymorphing. Str/Dex/Con only.
        public static int GetPrePolymorphAbilityScore(uint creature, AbilityType ability)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetPrePolymorphAbilityScore");
            NWNXPInvoke.NWNXPushInt((int)ability);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt();
        }

        // Gets the remaining spell slots (innate casting) for the provided creature for the provided classId and level.
        public static int GetRemainingSpellSlots(uint creature, ClassType classId, int level)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetRemainingSpellSlots");
            NWNXPInvoke.NWNXPushInt(level);
            NWNXPInvoke.NWNXPushInt((int)classId);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt();
        }

        // Sets the remaining spell slots (innate casting) for the provided creature for the provided classId and level.
        public static void SetRemainingSpellSlots(uint creature, ClassType classId, int level, int slots)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetRemainingSpellSlots");
            NWNXPInvoke.NWNXPushInt(slots);
            NWNXPInvoke.NWNXPushInt(level);
            NWNXPInvoke.NWNXPushInt((int)classId);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Remove a spell from creature's spellbook for class.
        public static void RemoveKnownSpell(uint creature, ClassType classId, int level, int spellId)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "RemoveKnownSpell");
            NWNXPInvoke.NWNXPushInt(spellId);
            NWNXPInvoke.NWNXPushInt(level);
            NWNXPInvoke.NWNXPushInt((int)classId);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Add a new spell to creature's spellbook for class.
        public static void AddKnownSpell(uint creature, ClassType classId, int level, int spellId)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "AddKnownSpell");
            NWNXPInvoke.NWNXPushInt(spellId);
            NWNXPInvoke.NWNXPushInt(level);
            NWNXPInvoke.NWNXPushInt((int)classId);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Gets the maximum count of spell slots for the proivded creature for the provided classId and level.
        public static int GetMaxSpellSlots(uint creature, ClassType classId, int level)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetMaxSpellSlots");
            NWNXPInvoke.NWNXPushInt(level);
            NWNXPInvoke.NWNXPushInt((int)classId);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt();
        }

        // Gets the maximum hit points for creature for level.
        public static int GetMaxHitPointsByLevel(uint creature, int level)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetMaxHitPointsByLevel");
            NWNXPInvoke.NWNXPushInt(level);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt();
        }

        // Sets the maximum hit points for creature for level to nValue.
        public static void SetMaxHitPointsByLevel(uint creature, int level, int value)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetMaxHitPointsByLevel");
            NWNXPInvoke.NWNXPushInt(value);
            NWNXPInvoke.NWNXPushInt(level);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Set creature's movement rate.
        public static void SetMovementRate(uint creature, MovementRate rate)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetMovementRate");
            NWNXPInvoke.NWNXPushInt((int)rate);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Returns the creature's current movement rate factor (base = 1.0)
        public static float GetMovementRateFactor(uint creature)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetMovementRateFactor");
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopFloat();
        }

        // Sets the creature's current movement rate factor (base = 1.0)
        public static void SetMovementRateFactor(uint creature, float factor)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetMovementRateFactor");
            NWNXPInvoke.NWNXPushFloat(factor);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Set creature's raw good/evil alignment value.
        public static void SetAlignmentGoodEvil(uint creature, int value)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetAlignmentGoodEvil");
            NWNXPInvoke.NWNXPushInt(value);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Set creature's raw law/chaos alignment value.
        public static void SetAlignmentLawChaos(uint creature, int value)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetAlignmentLawChaos");
            NWNXPInvoke.NWNXPushInt(value);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Set the base ranks in a skill for creature
        public static void SetSkillRank(uint creature, NWNSkillType skill, int rank)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetSkillRank");
            NWNXPInvoke.NWNXPushInt(rank);
            NWNXPInvoke.NWNXPushInt((int)skill);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Set the classId ID in a particular position for a creature.
        // Position should be 0, 1, or 2.
        // ClassID should be a valid ID number in classes.2da and be between 0 and 255.
        public static void SetClassByPosition(uint creature, int position, ClassType classId, bool updateLevels = true)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetClassByPosition");
            NWNXPInvoke.NWNXPushInt(updateLevels ? 1 : 0);
            NWNXPInvoke.NWNXPushInt((int)classId);
            NWNXPInvoke.NWNXPushInt(position);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
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
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetBaseAttackBonus");
            NWNXPInvoke.NWNXPushInt(bab);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Gets the creatures current attacks per round (using equipped weapon)
        // bBaseAPR - If true, will return the base attacks per round, based on BAB and
        //            equipped weapons, regardless of overrides set by
        //            calls to SetBaseAttackBonus() builtin function.
        public static int GetAttacksPerRound(uint creature, bool bBaseAPR)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetAttacksPerRound");
            NWNXPInvoke.NWNXPushInt(bBaseAPR ? 1 : 0);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt();
        }

        // Restore all creature feat uses
        public static void RestoreFeats(uint creature)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "RestoreFeats");
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Restore all creature special ability uses
        public static void RestoreSpecialAbilities(uint creature)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "RestoreSpecialAbilities");
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Restore uses for all items carried by the creature
        public static void RestoreItems(uint creature)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "RestoreItems");
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Sets the creature size. Use CREATURE_SIZE_* constants
        public static void SetSize(uint creature, CreatureSize creatureSize)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetSize");
            NWNXPInvoke.NWNXPushInt((int)creatureSize);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Gets the creature's remaining unspent skill points
        public static int GetSkillPointsRemaining(uint creature)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetSkillPointsRemaining");
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt();
        }


        // Sets the creature's remaining unspent skill points
        public static void SetSkillPointsRemaining(uint creature, int skillpoints)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetSkillPointsRemaining");
            NWNXPInvoke.NWNXPushInt(skillpoints);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Sets the creature's racial type
        public static void SetRacialType(uint creature, RacialType racialtype)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetRacialType");
            NWNXPInvoke.NWNXPushInt((int)racialtype);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Returns the creature's current movement type (MOVEMENT_TYPE_*)
        public static MovementType GetMovementType(uint creature)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetMovementType");
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
            return (MovementType)NWNXPInvoke.NWNXPopInt();
        }

        // Sets the maximum movement rate a creature can have while walking (not running)
        // This allows a creature with movement speed enhancemens to walk at a normal rate.
        // Setting the value to -1.0 will remove the cap.
        // Default value is 2000.0, which is the base human walk speed.
        public static void SetWalkRateCap(uint creature, float fWalkRate)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetWalkRateCap");
            NWNXPInvoke.NWNXPushFloat(fWalkRate);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Sets the creature's gold without sending a feedback message
        public static void SetGold(uint creature, int gold)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetGold");
            NWNXPInvoke.NWNXPushInt(gold);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Sets corpse decay time in milliseconds
        public static void SetCorpseDecayTime(uint creature, int decayTimeMs)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetCorpseDecayTime");
            NWNXPInvoke.NWNXPushInt(decayTimeMs);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Returns the creature's base save and any modifiers set in the toolset
        public static int GetBaseSavingThrow(uint creature, int which)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetBaseSavingThrow");
            NWNXPInvoke.NWNXPushInt(which);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt();
        }

        // Sets the base saving throw of the creature
        public static void SetBaseSavingThrow(uint creature, SavingThrow which, int value)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetBaseSavingThrow");
            NWNXPInvoke.NWNXPushInt(value);
            NWNXPInvoke.NWNXPushInt((int)which);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Add count levels of class to the creature, bypassing all validation
        // This will not work on player characters
        public static void LevelUp(uint creature, ClassType classId, int count = 1)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "LevelUp");
            NWNXPInvoke.NWNXPushInt(count);
            NWNXPInvoke.NWNXPushInt((int)classId);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Remove last count levels from a creature
        // This will not work on player characters
        public static void LevelDown(uint creature, int count = 1)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "LevelDown");
            NWNXPInvoke.NWNXPushInt(count);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Sets the creature's challenge rating
        public static void SetChallengeRating(uint creature, float fCR)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetChallengeRating");
            NWNXPInvoke.NWNXPushFloat(fCR);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
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
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetAttackBonus");
            NWNXPInvoke.NWNXPushInt(includeBaseAttackBonus ? 1 : 0);
            NWNXPInvoke.NWNXPushInt(isOffhand ? 1 : 0);
            NWNXPInvoke.NWNXPushInt(isTouchAttack ? 1 : 0);
            NWNXPInvoke.NWNXPushInt(isMelee ? 1 : 0);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt();
        }

        // Get highest level version of feat posessed by creature (e.g. for barbarian rage)
        public static int GetHighestLevelOfFeat(uint creature, int feat)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetHighestLevelOfFeat");
            NWNXPInvoke.NWNXPushInt(feat);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt();
        }

        // Get feat remaining uses of a creature
        public static int GetFeatRemainingUses(uint creature, FeatType feat)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetFeatRemainingUses");
            NWNXPInvoke.NWNXPushInt((int)feat);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt();
        }

        // Get feat total uses of a creature
        public static int GetFeatTotalUses(uint creature, FeatType feat)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetFeatTotalUses");
            NWNXPInvoke.NWNXPushInt((int)feat);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt();
        }

        // Set feat remaining uses of a creature
        public static void SetFeatRemainingUses(uint creature, FeatType feat, int uses)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetFeatRemainingUses");
            NWNXPInvoke.NWNXPushInt(uses);
            NWNXPInvoke.NWNXPushInt((int)feat);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Get total effect bonus
        public static int GetTotalEffectBonus(uint creature, BonusType bonusType = BonusType.Attack,
            uint target = NWScript.NWScript.OBJECT_INVALID, bool isElemental = false,
            bool isForceMax = false, int saveType = -1, int saveSpecificType = -1, NWNSkillType skill = NWNSkillType.Invalid,
            int abilityScore = -1, bool isOffhand = false)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetTotalEffectBonus");
            NWNXPInvoke.NWNXPushInt(isOffhand ? 1 : 0);
            NWNXPInvoke.NWNXPushInt(abilityScore);
            NWNXPInvoke.NWNXPushInt((int)skill);
            NWNXPInvoke.NWNXPushInt(saveSpecificType);
            NWNXPInvoke.NWNXPushInt(saveType);
            NWNXPInvoke.NWNXPushInt(isForceMax ? 1 : 0);
            NWNXPInvoke.NWNXPushInt(isElemental ? 1 : 0);
            NWNXPInvoke.NWNXPushObject(target);
            NWNXPInvoke.NWNXPushInt((int)bonusType);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt();
        }

        // Set the original first or last name of creature
        //
        // For PCs this will persist to the .bic file if saved. Requires a relog to update.
        public static void SetOriginalName(uint creature, string name, bool isLastName)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetOriginalName");
            NWNXPInvoke.NWNXPushInt(isLastName ? 1 : 0);
            NWNXPInvoke.NWNXPushString(name);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Get the original first or last name of creature
        public static string GetOriginalName(uint creature, bool isLastName)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetOriginalName");
            NWNXPInvoke.NWNXPushInt(isLastName ? 1 : 0);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopString();
        }

        // Set creature's spell resistance
        public static void SetSpellResistance(uint creature, int sr)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetSpellResistance");
            NWNXPInvoke.NWNXPushInt(sr);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        /// @brief Set creature's animal companion's name
        /// @param creature The master creature object.
        /// @param name The name to give their animal companion.
        public static void SetAnimalCompanionName(uint creature, string name)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetAnimalCompanionCreatureType");
            NWNXPInvoke.NWNXPushString(name);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        /// @brief Set creature's familiar's name
        /// @param creature The master creature object.
        /// @param name The name to give their familiar.
        public static void SetFamiliarName(uint creature, string name)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetFamiliarCreatureType");
            NWNXPInvoke.NWNXPushString(name);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        /// @brief Get whether the creature can be disarmed.
        /// @param creature The creature object.
        /// @return TRUE if the creature can be disarmed.
        public static int GetDisarmable(uint creature)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetDisarmable");
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt();
        }

        /// @brief Set whether a creature can be disarmed.
        /// @param creature The creature object.
        /// @param disarmable Set to TRUE if the creature can be disarmed.
        public static void SetDisarmable(uint creature, bool isDisarmable)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetDisarmable");
            NWNXPInvoke.NWNXPushInt(isDisarmable ? 1 : 0);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        /// @brief Sets one of creature's domains.
        /// @param creature The creature object.
        /// @param class The class id from classes.2da. (Not class index 0-2)
        /// @param index The first or second domain.
        /// @param domain The domain constant to set.
        public static void SetDomain(uint creature, ClassType @class, int index, int domain)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetDomain");
            NWNXPInvoke.NWNXPushInt(domain);
            NWNXPInvoke.NWNXPushInt(index);
            NWNXPInvoke.NWNXPushInt((int)@class);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        /// @brief Sets creature's specialist school.
        /// @param creature The creature object.
        /// @param class The class id from classes.2da. (Not class index 0-2)
        /// @param school The school constant.
        public static void SetSpecialization(uint creature, ClassType @class, int school)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetSpecialization");
            NWNXPInvoke.NWNXPushInt(school);
            NWNXPInvoke.NWNXPushInt((int)@class);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        /// @brief Sets oCreatures faction to be the faction with id nFactionId.
        /// @param oCreature The creature.
        /// @param nFactionId The faction id we want the creature to join.
        public static void SetFaction(uint creature, int factionId)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetFaction");
            NWNXPInvoke.NWNXPushInt(factionId);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }

        /// @brief Gets the faction id from oCreature
        /// @param oCreature the creature we wish to query against
        /// @return faction id as an integer, -1 when used against invalid creature or invalid object.
        public static int GetFaction(uint creature)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetFaction");
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt();
        }

        /// @brief Get whether a creature is flat-footed.
        /// @param The creature object.
        /// @return TRUE if the creature is flat-footed.
        public static bool GetFlatFooted(uint creature)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetFlatFooted");
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt() == 1;
        }

        /// @brief Serialize oCreature's quickbar to a base64 string
        /// @param oCreature The creature.
        /// @return A base64 string representation of oCreature's quickbar.
        public static string SerializeQuickbar(uint creature)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SerializeQuickbar");
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopString();
        }

        /// @brief Deserialize sSerializedQuickbar for oCreature
        /// @param oCreature The creature.
        /// @param sSerializedQuickbar A base64 string of a quickbar
        /// @return TRUE on success
        public static bool DeserializeQuickbar(uint creature, string serializedQuickbar)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "DeserializeQuickbar");
            NWNXPInvoke.NWNXPushString(serializedQuickbar);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt() == 1;
        }


        /// @brief Set the encounter source of oCreature.
        /// @param oCreature The target creature.
        /// @param oEncounter The source encounter
        public static void SetEncounter(uint oCreature, uint oEncounter)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetEncounter");

            NWNXPInvoke.NWNXPushObject(oEncounter);
            NWNXPInvoke.NWNXPushObject(oCreature);
            NWNXPInvoke.NWNXCallFunction();
        }

        /// @brief Get the encounter source of oCreature.
        /// @param oCreature The target creature.
        /// @return The encounter, OBJECT_INVALID if not part of an encounter or on error
        public static uint GetEncounter(uint oCreature)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetEncounter");

            NWNXPInvoke.NWNXPushObject(oCreature);
            NWNXPInvoke.NWNXCallFunction();

            return NWNXPInvoke.NWNXPopObject();
        }


        /// @brief Override the damage level of oCreature.
        /// @note Damage levels are the damage state under a creature's name, for example: 'Near Death'
        /// @param oCreature The target creature.
        /// @param nDamageLevel A damage level, see damagelevels.2da. Allowed values: 0-255 or -1 to remove the override.
        public static void OverrideDamageLevel(uint oCreature, int nDamageLevel)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "OverrideDamageLevel");

            NWNXPInvoke.NWNXPushInt(nDamageLevel);
            NWNXPInvoke.NWNXPushObject(oCreature);
            NWNXPInvoke.NWNXCallFunction();
        }


        /// @brief Get if oCreature is currently bartering.
        /// @param oCreature The target creature.
        /// @return TRUE if oCreature is bartering, FALSE if not or on error.
        public static bool GetIsBartering(uint oCreature)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetIsBartering");

            NWNXPInvoke.NWNXPushObject(oCreature);
            NWNXPInvoke.NWNXCallFunction();

            return NWNXPInvoke.NWNXPopInt() == 1;
        }


        /// @brief Sets caster level for the last item used. Use in a spellhook or spell event before to set caster level for any spells cast from the item.
        /// @param oCreature the creature who used the item.
        /// @param nCasterLvl the desired caster level.
        public static void SetLastItemCasterLevel(uint oCreature, int nCasterLvl)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetLastItemCasterLevel");

            NWNXPInvoke.NWNXPushInt(nCasterLvl);
            NWNXPInvoke.NWNXPushObject(oCreature);
            NWNXPInvoke.NWNXCallFunction();
        }

        /// @brief Gets the caster level of the last item used.
        /// @param oCreature the creature who used the item.
        /// @return returns the creatures last used item's level.
        public static int GetLastItemCasterLevel(uint oCreature)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetLastItemCasterLevel");

            NWNXPInvoke.NWNXPushObject(oCreature);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt();
        }

        /// @brief Gets the Armor classed of attacked against versus
        /// @param oAttacked The one being attacked
        /// @param oVersus The one doing the attacking
        /// @param nTouch TRUE for touch attacks
        /// @return -255 on Error, Flat footed AC if oVersus is invalid or the Attacked AC versus oVersus.
        public static int GetArmorClassVersus(uint oAttacked, uint oVersus, bool nTouch = false)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetArmorClassVersus");

            NWNXPInvoke.NWNXPushInt(nTouch ? 1 : 0);
            NWNXPInvoke.NWNXPushObject(oVersus);
            NWNXPInvoke.NWNXPushObject(oAttacked);
            NWNXPInvoke.NWNXCallFunction();

            return NWNXPInvoke.NWNXPopInt();
        }

        /// @brief Move a creature to limbo.
        /// @param oCreature The creature object.
        public static void JumpToLimbo(uint oCreature)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "JumpToLimbo");
            NWNXPInvoke.NWNXPushObject(oCreature);
            NWNXPInvoke.NWNXCallFunction();
        }

        /// @brief Sets the critical hit multiplier modifier for the creature
        /// @param oCreature The target creature
        /// @param nModifier The modifier to apply
        /// @param nHand 0 for all attacks, 1 for Mainhand, 2 for Offhand
        /// @param bPersist Whether the modifier should persist to .bic file if applicable
        /// @note Persistence is activated each server reset by first use of either 'SetCriticalMultiplier*' functions. Recommended to trigger on a dummy target OnModuleLoad to enable persistence.
        public static void SetCriticalMultiplierModifier(uint oCreature, int nModifier, int nHand = 0, bool bPersist = false, BaseItem baseItemType = BaseItem.Invalid)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetCriticalMultiplierModifier");

            NWNXPInvoke.NWNXPushInt((int)baseItemType);
            NWNXPInvoke.NWNXPushInt(bPersist ? 1 : 0);
            NWNXPInvoke.NWNXPushInt(nHand);
            NWNXPInvoke.NWNXPushInt(nModifier);
            NWNXPInvoke.NWNXPushObject(oCreature);

            NWNXPInvoke.NWNXCallFunction();
        }

        /// @brief Gets the critical hit multiplier modifier for the Creature
        /// @param oCreature The target creature
        /// @param nHand 0 for all attacks, 1 for Mainhand, 2 for Offhand
        /// @return the current critical hit multiplier modifier for the creature
        public static int GetCriticalMultiplierModifier(uint oCreature, int nHand = 0, BaseItem baseItemType = BaseItem.Invalid)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetCriticalMultiplierModifier");

            NWNXPInvoke.NWNXPushInt((int)baseItemType);
            NWNXPInvoke.NWNXPushInt(nHand);
            NWNXPInvoke.NWNXPushObject(oCreature);
            NWNXPInvoke.NWNXCallFunction();

            return NWNXPInvoke.NWNXPopInt();
        }

        public static void SetCriticalMultiplierOverride(uint oCreature, int nOverride, int nHand = 0, bool bPersist = false, BaseItem baseItemType = BaseItem.Invalid)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetCriticalMultiplierOverride");

            NWNXPInvoke.NWNXPushInt((int)baseItemType);
            NWNXPInvoke.NWNXPushInt(bPersist ? 1 : 0);
            NWNXPInvoke.NWNXPushInt(nHand);
            NWNXPInvoke.NWNXPushInt(nOverride);
            NWNXPInvoke.NWNXPushObject(oCreature);

            NWNXPInvoke.NWNXCallFunction();
        }

        public static int GetCriticalMultiplierOverride(uint oCreature, int nHand = 0, BaseItem baseItemType = BaseItem.Invalid)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetCriticalMultiplierOverride");

            NWNXPInvoke.NWNXPushInt((int)baseItemType);
            NWNXPInvoke.NWNXPushInt(nHand);
            NWNXPInvoke.NWNXPushObject(oCreature);
            NWNXPInvoke.NWNXCallFunction();

            return NWNXPInvoke.NWNXPopInt();
        }

        public static void SetCriticalRangeModifier(uint oCreature, int nModifier, int nHand = 0, bool bPersist = false, BaseItem baseItemType  = BaseItem.Invalid)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetCriticalRangeModifier");

            NWNXPInvoke.NWNXPushInt((int)baseItemType);
            NWNXPInvoke.NWNXPushInt(bPersist ? 1 : 0);
            NWNXPInvoke.NWNXPushInt(nHand);
            NWNXPInvoke.NWNXPushInt(nModifier);
            NWNXPInvoke.NWNXPushObject(oCreature);

            NWNXPInvoke.NWNXCallFunction();
        }

        public static int GetCriticalRangeModifier(uint oCreature, int nHand = 0, BaseItem baseItemType = BaseItem.Invalid)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetCriticalRangeModifier");

            NWNXPInvoke.NWNXPushInt((int)baseItemType);
            NWNXPInvoke.NWNXPushInt(nHand);
            NWNXPInvoke.NWNXPushObject(oCreature);
            NWNXPInvoke.NWNXCallFunction();

            return NWNXPInvoke.NWNXPopInt();
        }

        public static void SetCriticalRangeOverride(uint oCreature, int nOverride, int nHand = 0, bool bPersist = false, BaseItem baseItemType = BaseItem.Invalid)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetCriticalRangeOverride");

            NWNXPInvoke.NWNXPushInt((int)baseItemType);
            NWNXPInvoke.NWNXPushInt(bPersist ? 1 : 0);
            NWNXPInvoke.NWNXPushInt(nHand);
            NWNXPInvoke.NWNXPushInt(nOverride);
            NWNXPInvoke.NWNXPushObject(oCreature);

            NWNXPInvoke.NWNXCallFunction();
        }

        public static int GetCriticalRangeOverride(uint oCreature, int nHand = 0, BaseItem baseItemType = BaseItem.Invalid)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetCriticalRangeOverride");

            NWNXPInvoke.NWNXPushInt((int)baseItemType);
            NWNXPInvoke.NWNXPushInt(nHand);
            NWNXPInvoke.NWNXPushObject(oCreature);
            NWNXPInvoke.NWNXCallFunction();

            return NWNXPInvoke.NWNXPopInt();
        }

        public static void AddAssociate(uint oCreature, uint oAssociate, int nAssociateType)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "AddAssociate");

            NWNXPInvoke.NWNXPushInt(nAssociateType);
            NWNXPInvoke.NWNXPushObject(oAssociate);
            NWNXPInvoke.NWNXPushObject(oCreature);

            NWNXPInvoke.NWNXCallFunction();
        }

        public static int GetWalkAnimation(uint oCreature)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetWalkAnimation");

            NWNXPInvoke.NWNXPushObject(oCreature);
            NWNXPInvoke.NWNXCallFunction();

            return NWNXPInvoke.NWNXPopInt();
        }

        public static void SetWalkAnimation(uint oCreature, int nAnimation)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetWalkAnimation");
            NWNXPInvoke.NWNXPushInt(nAnimation);
            NWNXPInvoke.NWNXPushObject(oCreature);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static void SetAttackRollOverride(uint oCreature, int nRoll, int nModifier)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetAttackRollOverride");
            NWNXPInvoke.NWNXPushInt(nModifier);
            NWNXPInvoke.NWNXPushInt(nRoll);
            NWNXPInvoke.NWNXPushObject(oCreature);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static void SetParryAllAttacks(uint oCreature, bool bParry)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetParryAllAttacks");
            NWNXPInvoke.NWNXPushInt(bParry ? 1 : 0);
            NWNXPInvoke.NWNXPushObject(oCreature);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static bool GetNoPermanentDeath(uint oCreature)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetNoPermanentDeath");

            NWNXPInvoke.NWNXPushObject(oCreature);
            NWNXPInvoke.NWNXCallFunction();

            return NWNXPInvoke.NWNXPopInt() == 1;
        }

        public static void SetNoPermanentDeath(uint oCreature, bool bNoPermanentDeath)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetNoPermanentDeath");
            NWNXPInvoke.NWNXPushInt(bNoPermanentDeath ? 1 : 0);
            NWNXPInvoke.NWNXPushObject(oCreature);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static Vector3 ComputeSafeLocation(uint oCreature, Vector3 vPosition, float fRadius = 20.0f, bool bWalkStraightLineRequired = true)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "ComputeSafeLocation"); 
            
            NWNXPInvoke.NWNXPushInt(bWalkStraightLineRequired ? 1 : 0);
            NWNXPInvoke.NWNXPushFloat(fRadius);
            NWNXPInvoke.NWNXPushFloat(vPosition.X);
            NWNXPInvoke.NWNXPushFloat(vPosition.Y);
            NWNXPInvoke.NWNXPushFloat(vPosition.Z);
            NWNXPInvoke.NWNXPushObject(oCreature);
            NWNXPInvoke.NWNXCallFunction();

            return new Vector3
            {
                Z = NWNXPInvoke.NWNXPopFloat(),
                Y = NWNXPInvoke.NWNXPopFloat(),
                X = NWNXPInvoke.NWNXPopFloat()
            };
        }

        public static void DoPerceptionUpdateOnCreature(uint oCreature, uint oTargetCreature)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "DoPerceptionUpdateOnCreature");
            NWNXPInvoke.NWNXPushObject(oTargetCreature);
            NWNXPInvoke.NWNXPushObject(oCreature);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static float GetPersonalSpace(uint oCreature)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetPersonalSpace");

            NWNXPInvoke.NWNXPushObject(oCreature);
            NWNXPInvoke.NWNXCallFunction();

            return NWNXPInvoke.NWNXPopFloat();
        }

        public static void SetPersonalSpace(uint oCreature, float fPerspace)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetPersonalSpace");
            NWNXPInvoke.NWNXPushFloat(fPerspace);
            NWNXPInvoke.NWNXPushObject(oCreature);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static float GetCreaturePersonalSpace(uint oCreature)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetCreaturePersonalSpace");

            NWNXPInvoke.NWNXPushObject(oCreature);
            NWNXPInvoke.NWNXCallFunction();

            return NWNXPInvoke.NWNXPopFloat();
        }

        public static void SetCreaturePersonalSpace(uint oCreature, float fCrePerspace)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetCreaturePersonalSpace");
            NWNXPInvoke.NWNXPushFloat(fCrePerspace);
            NWNXPInvoke.NWNXPushObject(oCreature);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static float GetHeight(uint oCreature)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetHeight");

            NWNXPInvoke.NWNXPushObject(oCreature);
            NWNXPInvoke.NWNXCallFunction();

            return NWNXPInvoke.NWNXPopFloat();
        }

        public static void SetHeight(uint oCreature, float fHeight)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetHeight");
            NWNXPInvoke.NWNXPushFloat(fHeight);
            NWNXPInvoke.NWNXPushObject(oCreature);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static float GetHitDistance(uint oCreature)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetHitDistance");

            NWNXPInvoke.NWNXPushObject(oCreature);
            NWNXPInvoke.NWNXCallFunction();

            return NWNXPInvoke.NWNXPopFloat();
        }

        public static void SetHitDistance(uint oCreature, float fHitDist)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetHitDistance");
            NWNXPInvoke.NWNXPushFloat(fHitDist);
            NWNXPInvoke.NWNXPushObject(oCreature);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static float GetPreferredAttackDistance(uint oCreature)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetPreferredAttackDistance");

            NWNXPInvoke.NWNXPushObject(oCreature);
            NWNXPInvoke.NWNXCallFunction();

            return NWNXPInvoke.NWNXPopFloat();
        }

        public static void SetPreferredAttackDistance(uint oCreature, float fPrefAtckDist)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetPreferredAttackDistance");
            NWNXPInvoke.NWNXPushFloat(fPrefAtckDist);
            NWNXPInvoke.NWNXPushObject(oCreature);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static int GetArmorCheckPenalty(uint oCreature)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetArmorCheckPenalty");

            NWNXPInvoke.NWNXPushObject(oCreature);
            NWNXPInvoke.NWNXCallFunction();

            return NWNXPInvoke.NWNXPopInt();
        }

        public static int GetShieldCheckPenalty(uint oCreature)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetShieldCheckPenalty");

            NWNXPInvoke.NWNXPushObject(oCreature);
            NWNXPInvoke.NWNXCallFunction();

            return NWNXPInvoke.NWNXPopInt();
        }

        public static void SetBypassEffectImmunity(uint oCreature, int nImmunityType, int nChance = 100, bool bPersist = false)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetBypassEffectImmunity");
            NWNXPInvoke.NWNXPushInt(bPersist ? 1 : 0);
            NWNXPInvoke.NWNXPushInt(nChance);
            NWNXPInvoke.NWNXPushInt(nImmunityType);
            NWNXPInvoke.NWNXPushObject(oCreature);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static int GetBypassEffectImmunity(uint oCreature, int nImmunityType)        
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetBypassEffectImmunity");

            NWNXPInvoke.NWNXPushInt(nImmunityType);
            NWNXPInvoke.NWNXPushObject(oCreature);
            NWNXPInvoke.NWNXCallFunction();

            return NWNXPInvoke.NWNXPopInt();
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
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetNumberOfBonusSpells");
            NWNXPInvoke.NWNXPushInt(spellLevel);
            NWNXPInvoke.NWNXPushInt(multiClass);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt();
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
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "ModifyNumberBonusSpells");
            NWNXPInvoke.NWNXPushInt(delta);
            NWNXPInvoke.NWNXPushInt(spellLevel);
            NWNXPInvoke.NWNXPushInt(multiClass);
            NWNXPInvoke.NWNXPushObject(creature);
            NWNXPInvoke.NWNXCallFunction();
        }
    }
}