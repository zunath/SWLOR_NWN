using NWN;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.NWNX.Contracts;

namespace SWLOR.Game.Server.NWNX
{
    public class NWNXCreature : NWNXBase, INWNXCreature
    {
        public NWNXCreature(INWScript script)
            : base(script)
        {
        }

        private const string NWNX_Creature = "NWNX_Creature";

        // Gives the provided creature the provided feat.
        public void AddFeat(NWCreature creature, int feat)
        {
            string sFunc = "AddFeat";
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, feat);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        // Gives the provided creature the provided feat.
        // Adds the feat to the stat list at the provided level.
        public void AddFeatByLevel(NWCreature creature, int feat, int level)
        {
            string sFunc = "AddFeatByLevel";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, level);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, feat);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        // Removes from the provided creature the provided feat.
        public void RemoveFeat(NWCreature creature, int feat)
        {
            string sFunc = "RemoveFeat";
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, feat);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        public int GetKnowsFeat(NWCreature creature, int feat)
        {
            string sFunc = "GetKnowsFeat";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, feat);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        // Returns the count of feats learned at the provided level.
        public int GetFeatCountByLevel(NWCreature creature, int level)
        {
            string sFunc = "GetFeatCountByLevel";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, level);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        // Returns the feat learned at the provided level at the provided index.
        // Index bounds: 0 <= index < GetFeatCountByLevel(creature, level).
        public int GetFeatByLevel(NWCreature creature, int level, int index)
        {
            string sFunc = "GetFeatByLevel";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, index);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, level);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        // Returns the total number of feats known by creature
        public int GetFeatCount(NWCreature creature)
        {
            string sFunc = "GetFeatCount";

            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        // Returns the creature's feat at a given index
        // Index bounds: 0 <= index < GetFeatCount(creature);
        public int GetFeatByIndex(NWCreature creature, int index)
        {
            string sFunc = "GetFeatByIndex";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, index);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        // Returns TRUE if creature meets all requirements to take given feat
        public int GetMeetsFeatRequirements(NWCreature creature, int feat)
        {
            string sFunc = "GetMeetsFeatRequirements";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, feat);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        // Returns the special ability of the provided creature at the provided index.
        // Index bounds: 0 <= index < GetSpecialAbilityCount(creature).
        public SpecialAbilitySlot GetSpecialAbility(NWCreature creature, int index)
        {
            string sFunc = "GetSpecialAbility";

            SpecialAbilitySlot ability = new SpecialAbilitySlot();

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, index);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);

            ability.Level = NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
            ability.Ready = NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
            ability.ID = NWNX_GetReturnValueInt(NWNX_Creature, sFunc);

            return ability;
        }

        // Returns the count of special ability count of the provided creature.
        public int GetSpecialAbilityCount(NWCreature creature)
        {
            string sFunc = "GetSpecialAbilityCount";

            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);
            NWNX_CallFunction(NWNX_Creature, sFunc);

            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        // Adds the provided special ability to the provided creature.
        public void AddSpecialAbility(NWCreature creature, SpecialAbilitySlot ability)
        {
            string sFunc = "AddSpecialAbility";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, ability.ID);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, ability.Ready);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, ability.Level);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        // Removes the provided special ability from the provided creature.
        // Index bounds: 0 <= index < GetSpecialAbilityCount(creature).
        public void RemoveSpecialAbility(NWCreature creature, int index)
        {
            string sFunc = "RemoveSpecialAbility";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, index);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        // Sets the special ability at the provided index for the provided creature to the provided ability.
        // Index bounds: 0 <= index < GetSpecialAbilityCount(creature).
        public void SetSpecialAbility(NWCreature creature, int index, SpecialAbilitySlot ability)
        {
            string sFunc = "SetSpecialAbility";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, ability.ID);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, ability.Ready);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, ability.Level);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        // Returns the classId taken by the provided creature at the provided level.
        public int GetClassByLevel(NWCreature creature, int level)
        {
            string sFunc = "GetClassByLevel";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, level);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        // Sets the base AC for the provided creature.
        public void SetBaseAC(NWCreature creature, int ac)
        {
            string sFunc = "SetBaseAC";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, ac);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        // Returns the base AC for the provided creature.
        public int GetBaseAC(NWCreature creature)
        {
            string sFunc = "GetBaseAC";

            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        // DEPRECATED. Please use SetRawAbilityScore now. This will be removed in future NWNX releases.
        // Sets the provided ability score of provided creature to the provided value.
        public void SetAbilityScore(NWCreature creature, int ability, int value)
        {
            _.WriteTimestampedLogEntry("NWNX_Creature: SetAbilityScore() is deprecated. Use native SetRawAbilityScore() instead");
            SetRawAbilityScore(creature, ability, value);
        }

        // Sets the provided ability score of provided creature to the provided value. Does not apply racial bonuses/penalties.
        public void SetRawAbilityScore(NWCreature creature, int ability, int value)
        {
            string sFunc = "SetRawAbilityScore";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, value);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, ability);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        // Gets the provided ability score of provided creature. Does not apply racial bonuses/penalties.
        public int GetRawAbilityScore(NWCreature creature, int ability)
        {
            string sFunc = "GetRawAbilityScore";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, ability);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        // Adjusts the provided ability score of a provided creature. Does not apply racial bonuses/penalties.
        public void ModifyRawAbilityScore(NWCreature creature, int ability, int modifier)
        {
            string sFunc = "ModifyRawAbilityScore";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, modifier);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, ability);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        // Gets the memorized spell of the provided creature for the provided class, level, and index.
        // Index bounds: 0 <= index < GetMemorizedSpellCountByLevel(creature, class, level).
        public MemorizedSpellSlot GetMemorizedSpell(NWCreature creature, int classId, int level, int index)
        {
            string sFunc = "GetMemorisedSpell";
            MemorizedSpellSlot spell = new MemorizedSpellSlot();

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, index);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, level);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, classId);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);

            spell.Domain = NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
            spell.Meta = NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
            spell.Ready = NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
            spell.ID = NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
            return spell;
        }

        // Gets the count of memorized spells of the provided classId and level belonging to the provided creature.
        public int GetMemorizedSpellCountByLevel(NWCreature creature, int classId, int level)
        {
            string sFunc = "GetMemorisedSpellCountByLevel";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, level);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, classId);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        // Sets the memorized spell of the provided creature for the provided class, level, and index.
        // Index bounds: 0 <= index < GetMemorizedSpellCountByLevel(creature, class, level).
        public void SetMemorizedSpell(NWCreature creature, int classId, int level, int index, MemorizedSpellSlot spell)
        {
            string sFunc = "SetMemorisedSpell";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, spell.ID);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, spell.Ready);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, spell.Meta);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, spell.Domain);

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, index);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, level);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, classId);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        // Gets the remaining spell slots (innate casting) for the provided creature for the provided classId and level.
        public int GetRemainingSpellSlots(NWCreature creature, int classId, int level)
        {
            string sFunc = "GetRemainingSpellSlots";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, level);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, classId);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        // Sets the remaining spell slots (innate casting) for the provided creature for the provided classId and level.
        public void SetRemainingSpellSlots(NWCreature creature, int classId, int level, int slots)
        {
            string sFunc = "SetRemainingSpellSlots";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, slots);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, level);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, classId);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        // Get the spell at index in level in creature's spellbook from class.
        public int GetKnownSpell(NWCreature creature, int classId, int level, int index)
        {
            string sFunc = "GetKnownSpell";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, index);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, level);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, classId);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        public int GetKnownSpellCount(NWCreature creature, int classId, int level)
        {
            string sFunc = "GetKnownSpellCount";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, level);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, classId);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        // Remove a spell from creature's spellbook for class.
        public void RemoveKnownSpell(NWCreature creature, int classId, int level, int spellId)
        {
            string sFunc = "RemoveKnownSpell";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, spellId);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, level);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, classId);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        // Add a new spell to creature's spellbook for class.
        public void AddKnownSpell(NWCreature creature, int classId, int level, int spellId)
        {
            string sFunc = "AddKnownSpell";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, spellId);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, level);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, classId);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        // Gets the maximum count of spell slots for the proivded creature for the provided classId and level.
        public int GetMaxSpellSlots(NWCreature creature, int classId, int level)
        {
            string sFunc = "GetMaxSpellSlots";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, level);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, classId);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        // Gets the maximum hit points for creature for level.
        public int GetMaxHitPointsByLevel(NWCreature creature, int level)
        {
            string sFunc = "GetMaxHitPointsByLevel";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, level);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        // Sets the maximum hit points for creature for level to nValue.
        public void SetMaxHitPointsByLevel(NWCreature creature, int level, int value)
        {
            string sFunc = "SetMaxHitPointsByLevel";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, value);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, level);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        // Set creature's movement rate.
        public void SetMovementRate(NWCreature creature, int rate)
        {
            string sFunc = "SetMovementRate";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, rate);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        // Set creature's raw good/evil alignment value.
        public void SetAlignmentGoodEvil(NWCreature creature, int value)
        {
            string sFunc = "SetAlignmentGoodEvil";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, value);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        // Set creature's raw law/chaos alignment value.
        public void SetAlignmentLawChaos(NWCreature creature, int value)
        {
            string sFunc = "SetAlignmentLawChaos";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, value);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        // Gets one of creature's cleric domains (either 1 or 2).
        public int GetClericDomain(NWCreature creature, int index)
        {
            string sFunc = "GetClericDomain";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, index);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        // Sets one of creature's cleric domains (either 1 or 2).
        public void SetClericDomain(NWCreature creature, int index, int domain)
        {
            string sFunc = "SetClericDomain";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, domain);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, index);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        // Gets whether or not creature has a specialist school of wizardry.
        public int GetWizardSpecialization(NWCreature creature)
        {
            string sFunc = "GetWizardSpecialization";

            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        // Sets creature's wizard specialist school.
        public void SetWizardSpecialization(NWCreature creature, int school)
        {
            string sFunc = "SetWizardSpecialization";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, school);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        // Get the soundset index for creature.
        public int GetSoundset(NWCreature creature)
        {
            string sFunc = "GetSoundset";

            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        // Set the soundset index for creature.
        public void SetSoundset(NWCreature creature, int soundset)
        {
            string sFunc = "SetSoundset";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, soundset);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        // Set the base ranks in a skill for creature
        public void SetSkillRank(NWCreature creature, int skill, int rank)
        {
            string sFunc = "SetSkillRank";
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, rank);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, skill);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        // Set the classId ID in a particular position for a creature.
        // Position should be 0, 1, or 2.
        // ClassID should be a valid ID number in classes.2da and be between 0 and 255.
        public void SetClassByPosition(NWCreature creature, int position, int classId)
        {
            string sFunc = "SetClassByPosition";
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, classId);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, position);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        // Set creature's base attack bonus (BAB)
        // Modifying the BAB will also affect the creature's attacks per round and its
        // eligability for feats, prestige classes, etc.
        // The BAB value should be between 0 and 254.
        // Setting BAB to 0 will cause the creature to revert to its original BAB based
        // on its classes and levels. A creature can never have an actual BAB of zero.
        // NOTE: The base game has a function SetBaseAttackBonus(), which actually sets
        //       the bonus attacks per round for a creature, not the BAB.
        public void SetBaseAttackBonus(NWCreature creature, int bab)
        {
            string sFunc = "SetBaseAttackBonus";
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, bab);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        // Gets the creatures current attacks per round (using equipped weapon)
        // bBaseAPR - If true, will return the base attacks per round, based on BAB and
        //            equipped weapons, regardless of overrides set by
        //            calls to SetBaseAttackBonus() builtin function.
        public int GetAttacksPerRound(NWCreature creature, int bBaseAPR)
        {
            string sFunc = "GetAttacksPerRound";
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, bBaseAPR);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        // Sets the creature gender
        public void SetGender(NWCreature creature, int gender)
        {
            string sFunc = "SetGender";
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, gender);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        // Restore all creature feat uses
        public void RestoreFeats(NWCreature creature)
        {
            string sFunc = "RestoreFeats";
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        // Restore all creature special ability uses
        public void RestoreSpecialAbilities(NWCreature creature)
        {
            string sFunc = "RestoreSpecialAbilities";
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        // Restore all creature spells per day for given level.
        // If level is -1, all spells are restored
        public void RestoreSpells(NWCreature creature, int level)
        {
            string sFunc = "RestoreSpells";
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, level);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        // Restore uses for all items carried by the creature
        public void RestoreItems(NWCreature creature)
        {
            string sFunc = "RestoreItems";
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        // Sets the creature size. Use CREATURE_SIZE_* constants
        public void SetSize(NWCreature creature, int size)
        {
            string sFunc = "SetSize";
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, size);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        // Gets the creature's remaining unspent skill points
        public int GetSkillPointsRemaining(NWCreature creature)
        {
            string sFunc = "GetSkillPointsRemaining";
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }


        // Sets the creature's remaining unspent skill points
        public void SetSkillPointsRemaining(NWCreature creature, int skillpoints)
        {
            string sFunc = "SetSkillPointsRemaining";
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, skillpoints);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        // Sets the creature's racial type
        public void SetRacialType(NWCreature creature, int racialtype)
        {
            string sFunc = "SetRacialType";
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, racialtype);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        // Returns the creature's current movement type (MOVEMENT_TYPE_*)
        public int GetMovementType(NWCreature creature)
        {
            string sFunc = "GetMovementType";
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        // Sets the maximum movement rate a creature can have while walking (not running)
        // This allows a creature with movement speed enhancemens to walk at a normal rate.
        // Setting the value to -1.0 will remove the cap.
        // Default value is 2000.0, which is the base human walk speed.
        public void SetWalkRateCap(NWCreature creature, float fWalkRate)
        {
            string sFunc = "SetWalkRateCap";
            NWNX_PushArgumentFloat(NWNX_Creature, sFunc, fWalkRate);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        public void SetGold(NWCreature creature, int gold)
        {
            string sFunc = "SetGold";
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, gold);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        public void SetCorpseDecayTime(NWCreature creature, int nDecayTime)
        {
            string sFunc = "SetCorpseDecayTime";
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, nDecayTime);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        public int GetBaseSavingThrow(NWCreature creature, int which)
        {
            string sFunc = "GetBaseSavingThrow";
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, which);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        public void SetBaseSavingThrow(NWCreature creature, int which, int value)
        {
            string sFunc = "SetBaseSavingThrow";
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, value);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, which);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        public void LevelUp(NWCreature creature, int @class, int count = 1)
        {
            string sFunc = "LevelUp";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, count);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, @class);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        public void LevelDown(NWCreature creature, int count = 1)
        {
            string sFunc = "LevelDown";
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, count);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        public void SetChallengeRating(NWCreature creature, float fCR)
        {
            string sFunc = "SetChallengeRating";
            NWNX_PushArgumentFloat(NWNX_Creature, sFunc, fCR);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature.Object);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }
    }
}
