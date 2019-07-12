using NWN;
using SWLOR.Game.Server.GameObject;
using static NWN._;
using static SWLOR.Game.Server.NWNX.NWNXCore;

namespace SWLOR.Game.Server.NWNX
{
    public static class NWNXCreature
    {
        private const string NWNX_Creature = "NWNX_Creature";

        /// <summary>
        /// Gives the provided creature the provided feat.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="feat"></param>
        public static void AddFeat(NWCreature creature, int feat)
        {
            string sFunc = "AddFeat";
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, feat);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Gives the provided creature the provided feat.
        /// Adds the feat to the stat list at the provided level.</summary>
        /// <param name="creature"></param>
        /// <param name="feat"></param>
        /// <param name="level"></param>
        public static void AddFeatByLevel(NWCreature creature, int feat, int level)
        {
            string sFunc = "AddFeatByLevel";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, level);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, feat);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Removes from the provided creature the provided feat.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="feat"></param>
        public static void RemoveFeat(NWCreature creature, int feat)
        {
            string sFunc = "RemoveFeat";
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, feat);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Gets whether creature knows feat.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="feat"></param>
        /// <returns></returns>
        public static int GetKnowsFeat(NWCreature creature, int feat)
        {
            string sFunc = "GetKnowsFeat";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, feat);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Returns the count of feats learned at the provided level.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static int GetFeatCountByLevel(NWCreature creature, int level)
        {
            string sFunc = "GetFeatCountByLevel";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, level);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Returns the feat learned at the provided level at the provided index.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="level"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static int GetFeatByLevel(NWCreature creature, int level, int index)
        {
            string sFunc = "GetFeatByLevel";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, index);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, level);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Returns the total number of feats known by creature
        /// </summary>
        /// <param name="creature"></param>
        /// <returns></returns>
        public static int GetFeatCount(NWCreature creature)
        {
            string sFunc = "GetFeatCount";

            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Returns the creature's feat at a given index
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static int GetFeatByIndex(NWCreature creature, int index)
        {
            string sFunc = "GetFeatByIndex";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, index);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Returns TRUE if creature meets all requirements to take given feat
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="feat"></param>
        /// <returns></returns>
        public static int GetMeetsFeatRequirements(NWCreature creature, int feat)
        {
            string sFunc = "GetMeetsFeatRequirements";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, feat);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Returns the special ability of the provided creature at the provided index.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static SpecialAbilitySlot GetSpecialAbility(NWCreature creature, int index)
        {
            string sFunc = "GetSpecialAbility";

            SpecialAbilitySlot ability = new SpecialAbilitySlot();

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, index);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);

            ability.Level = NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
            ability.Ready = NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
            ability.ID = NWNX_GetReturnValueInt(NWNX_Creature, sFunc);

            return ability;
        }

        /// <summary>
        /// Returns the count of special ability count of the provided creature.
        /// </summary>
        /// <param name="creature"></param>
        /// <returns></returns>
        public static int GetSpecialAbilityCount(NWCreature creature)
        {
            string sFunc = "GetSpecialAbilityCount";

            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);
            NWNX_CallFunction(NWNX_Creature, sFunc);

            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Adds the provided special ability to the provided creature.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="ability"></param>
        public static void AddSpecialAbility(NWCreature creature, SpecialAbilitySlot ability)
        {
            string sFunc = "AddSpecialAbility";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, ability.ID);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, ability.Ready);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, ability.Level);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Removes the provided special ability from the provided creature.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="index"></param>
        public static void RemoveSpecialAbility(NWCreature creature, int index)
        {
            string sFunc = "RemoveSpecialAbility";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, index);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Sets the special ability at the provided index for the provided creature to the provided ability.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="index"></param>
        /// <param name="ability"></param>
        public static void SetSpecialAbility(NWCreature creature, int index, SpecialAbilitySlot ability)
        {
            string sFunc = "SetSpecialAbility";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, ability.ID);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, ability.Ready);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, ability.Level);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, index);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Returns the class taken by the provided creature at the provided level.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static int GetClassByLevel(NWCreature creature, int level)
        {
            string sFunc = "GetClassByLevel";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, level);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Sets the base AC for the provided creature.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="ac"></param>
        public static void SetBaseAC(NWCreature creature, int ac)
        {
            string sFunc = "SetBaseAC";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, ac);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Returns the base AC for the provided creature.
        /// </summary>
        /// <param name="creature"></param>
        /// <returns></returns>
        public static int GetBaseAC(NWCreature creature)
        {
            string sFunc = "GetBaseAC";

            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Sets the provided ability score of provided creature to the provided value. Does not apply racial bonuses/penalties.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="ability"></param>
        /// <param name="value"></param>
        public static void SetRawAbilityScore(NWCreature creature, int ability, int value)
        {
            string sFunc = "SetRawAbilityScore";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, value);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, ability);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Gets the provided ability score of provided creature. Does not apply racial bonuses/penalties.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="ability"></param>
        /// <returns></returns>
        public static int GetRawAbilityScore(NWCreature creature, int ability)
        {
            string sFunc = "GetRawAbilityScore";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, ability);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Adjusts the provided ability score of a provided creature. Does not apply racial bonuses/penalties.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="ability"></param>
        /// <param name="modifier"></param>
        public static void ModifyRawAbilityScore(NWCreature creature, int ability, int modifier)
        {
            string sFunc = "ModifyRawAbilityScore";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, modifier);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, ability);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Gets the memorised spell of the provided creature for the provided class, level, and index.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="class"></param>
        /// <param name="level"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static MemorizedSpellSlot GetMemorisedSpell(NWCreature creature, int @class, int level, int index)
        {
            string sFunc = "GetMemorisedSpell";
            MemorizedSpellSlot spell = new MemorizedSpellSlot();

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, index);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, level);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, @class);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);

            spell.Domain = NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
            spell.Meta = NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
            spell.Ready = NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
            spell.ID = NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
            return spell;
        }

        /// <summary>
        /// Gets the count of memorised spells of the provided class and level belonging to the provided creature.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="class"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static int GetMemorisedSpellCountByLevel(NWCreature creature, int @class, int level)
        {
            string sFunc = "GetMemorisedSpellCountByLevel";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, level);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, @class);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Sets the memorised spell of the provided creature for the provided class, level, and index.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="class"></param>
        /// <param name="level"></param>
        /// <param name="index"></param>
        /// <param name="spell"></param>
        public static void SetMemorisedSpell(NWCreature creature, int @class, int level, int index, MemorizedSpellSlot spell)
        {
            string sFunc = "SetMemorisedSpell";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, spell.ID);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, spell.Ready);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, spell.Meta);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, spell.Domain);

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, index);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, level);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, @class);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Gets the remaining spell slots (innate casting) for the provided creature for the provided class and level.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="class"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static int GetRemainingSpellSlots(NWCreature creature, int @class, int level)
        {
            string sFunc = "GetRemainingSpellSlots";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, level);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, @class);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Sets the remaining spell slots (innate casting) for the provided creature for the provided class and level.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="class"></param>
        /// <param name="level"></param>
        /// <param name="slots"></param>
        public static void SetRemainingSpellSlots(NWCreature creature, int @class, int level, int slots)
        {
            string sFunc = "SetRemainingSpellSlots";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, slots);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, level);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, @class);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Get the spell at index in level in creature's spellbook from class.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="class"></param>
        /// <param name="level"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static int GetKnownSpell(NWCreature creature, int @class, int level, int index)
        {
            string sFunc = "GetKnownSpell";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, index);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, level);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, @class);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Returns the number of known spells.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="class"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static int GetKnownSpellCount(NWCreature creature, int @class, int level)
        {
            string sFunc = "GetKnownSpellCount";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, level);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, @class);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Remove a spell from creature's spellbook for class.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="class"></param>
        /// <param name="level"></param>
        /// <param name="spellId"></param>
        public static void RemoveKnownSpell(NWCreature creature, int @class, int level, int spellId)
        {
            string sFunc = "RemoveKnownSpell";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, spellId);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, level);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, @class);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Add a new spell to creature's spellbook for class.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="class"></param>
        /// <param name="level"></param>
        /// <param name="spellId"></param>
        public static void AddKnownSpell(NWCreature creature, int @class, int level, int spellId)
        {
            string sFunc = "AddKnownSpell";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, spellId);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, level);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, @class);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Clear a specific spell from the creature's spellbook for class
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="class"></param>
        /// <param name="spellId"></param>
        public static void ClearMemorisedKnownSpells(NWCreature creature, int @class, int spellId)
        {
            string sFunc = "ClearMemorisedKnownSpells";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, spellId);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, @class);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Clear the memorised spell of the provided creature for the provided class, level and index. */
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="class"></param>
        /// <param name="level"></param>
        /// <param name="index"></param>
        public static void ClearMemorisedSpell(NWCreature creature, int @class, int level, int index)
        {
            string sFunc = "ClearMemorisedSpell";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, index);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, level);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, @class);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Gets the maximum count of spell slots for the proivded creature for the provided class and level.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="class"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static int GetMaxSpellSlots(NWCreature creature, int @class, int level)
        {
            string sFunc = "GetMaxSpellSlots";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, level);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, @class);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Gets the maximum hit points for creature for level.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static int GetMaxHitPointsByLevel(NWCreature creature, int level)
        {
            string sFunc = "GetMaxHitPointsByLevel";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, level);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Sets the maximum hit points for creature for level to nValue.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="level"></param>
        /// <param name="value"></param>
        public static void SetMaxHitPointsByLevel(NWCreature creature, int level, int value)
        {
            string sFunc = "SetMaxHitPointsByLevel";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, value);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, level);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Set creature's movement rate.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="rate"></param>
        public static void SetMovementRate(NWCreature creature, int rate)
        {
            string sFunc = "SetMovementRate";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, rate);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Set creature's raw good/evil alignment value.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="value"></param>
        public static void SetAlignmentGoodEvil(NWCreature creature, int value)
        {
            string sFunc = "SetAlignmentGoodEvil";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, value);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Set creature's raw law/chaos alignment value.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="value"></param>
        public static void SetAlignmentLawChaos(NWCreature creature, int value)
        {
            string sFunc = "SetAlignmentLawChaos";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, value);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Gets one of creature's cleric domains (either 1 or 2).
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static int GetClericDomain(NWCreature creature, int index)
        {
            string sFunc = "GetClericDomain";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, index);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Sets one of creature's cleric domains (either 1 or 2).
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="index"></param>
        /// <param name="domain"></param>
        public static void SetClericDomain(NWCreature creature, int index, int domain)
        {
            string sFunc = "SetClericDomain";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, domain);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, index);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Gets whether or not creature has a specialist school of wizardry.
        /// </summary>
        /// <param name="creature"></param>
        /// <returns></returns>
        public static int GetWizardSpecialization(NWCreature creature)
        {
            string sFunc = "GetWizardSpecialization";

            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Sets creature's wizard specialist school.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="school"></param>
        public static void SetWizardSpecialization(NWCreature creature, int school)
        {
            string sFunc = "SetWizardSpecialization";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, school);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Get the soundset index for creature.
        /// </summary>
        /// <param name="creature"></param>
        /// <returns></returns>
        public static int GetSoundset(NWCreature creature)
        {
            string sFunc = "GetSoundset";

            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Set the soundset index for creature.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="soundset"></param>
        public static void SetSoundset(NWCreature creature, int soundset)
        {
            string sFunc = "SetSoundset";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, soundset);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Set the base ranks in a skill for creature
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="skill"></param>
        /// <param name="rank"></param>
        public static void SetSkillRank(NWCreature creature, int skill, int rank)
        {
            string sFunc = "SetSkillRank";
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, rank);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, skill);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Set the class ID in a particular position for a creature.
        /// Position should be 0, 1, or 2.
        /// ClassID should be a valid ID number in classes.2da and be between 0 and 255.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="position"></param>
        /// <param name="classID"></param>
        public static void SetClassByPosition(NWCreature creature, int position, int @classID)
        {
            string sFunc = "SetClassByPosition";
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, @classID);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, position);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }
        /// <summary>
        /// Set the level at the given position for a creature. A creature should already
        /// have a class in that position.
        /// Position should be 0, 1, or 2.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="position"></param>
        /// <param name="level"></param>
        public static void SetLevelByPosition(NWCreature creature, int position, int level)
        {
            string sFunc = "SetLevelByPosition";
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, level);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, position);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Set creature's base attack bonus (BAB)
        /// Modifying the BAB will also affect the creature's attacks per round and its
        /// eligability for feats, prestige classes, etc.
        /// The BAB value should be between 0 and 254.
        /// Setting BAB to 0 will cause the creature to revert to its original BAB based
        /// on its classes and levels. A creature can never have an actual BAB of zero.
        /// NOTE: The base game has a function SetBaseAttackBonus(), which actually sets
        ///       the bonus attacks per round for a creature, not the BAB.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="bab"></param>
        public static void SetBaseAttackBonus(NWCreature creature, int bab)
        {
            string sFunc = "SetBaseAttackBonus";
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, bab);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Gets the creatures current attacks per round (using equipped weapon)
        /// bBaseAPR - If true, will return the base attacks per round, based on BAB and
        ///            equipped weapons, regardless of overrides set by
        ///            calls to SetBaseAttackBonus() builtin function.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="bBaseAPR"></param>
        /// <returns></returns>
        public static int GetAttacksPerRound(NWCreature creature, int bBaseAPR = FALSE)
        {
            string sFunc = "GetAttacksPerRound";
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, bBaseAPR);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Sets the creature gender
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="gender"></param>
        public static void SetGender(NWCreature creature, int gender)
        {
            string sFunc = "SetGender";
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, gender);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Restore all creature feat uses
        /// </summary>
        /// <param name="creature"></param>
        public static void RestoreFeats(NWCreature creature)
        {
            string sFunc = "RestoreFeats";
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Restore all creature special ability uses
        /// </summary>
        /// <param name="creature"></param>
        public static void RestoreSpecialAbilities(NWCreature creature)
        {
            string sFunc = "RestoreSpecialAbilities";
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Restore all creature spells per day for given level.
        /// If level is -1, all spells are restored
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="level"></param>
        public static void RestoreSpells(NWCreature creature, int level = -1)
        {
            string sFunc = "RestoreSpells";
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, level);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Restore uses for all items carried by the creature
        /// </summary>
        /// <param name="creature"></param>
        public static void RestoreItems(NWCreature creature)
        {
            string sFunc = "RestoreItems";
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Sets the creature size. Use CREATURE_SIZE_* constants
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="size"></param>
        public static void SetSize(NWCreature creature, int size)
        {
            string sFunc = "SetSize";
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, size);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Gets the creature's remaining unspent skill points
        /// </summary>
        /// <param name="creature"></param>
        /// <returns></returns>
        public static int GetSkillPointsRemaining(NWCreature creature)
        {
            string sFunc = "GetSkillPointsRemaining";
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }
        
        /// <summary>
        /// Sets the creature's remaining unspent skill points
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="skillpoints"></param>
        public static void SetSkillPointsRemaining(NWCreature creature, int skillpoints)
        {
            string sFunc = "SetSkillPointsRemaining";
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, skillpoints);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Sets the creature's racial type
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="racialtype"></param>
        public static void SetRacialType(NWCreature creature, int racialtype)
        {
            string sFunc = "SetRacialType";
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, racialtype);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Returns the creature's current movement type (NWNX_CREATURE_MOVEMENT_TYPE_*)
        /// </summary>
        /// <param name="creature"></param>
        /// <returns></returns>
        public static int GetMovementType(NWCreature creature)
        {
            string sFunc = "GetMovementType";
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Sets the maximum movement rate a creature can have while walking (not running)
        /// This allows a creature with movement speed enhancemens to walk at a normal rate.
        /// Setting the value to -1.0 will remove the cap.
        /// Default value is 2000.0, which is the base human walk speed.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="fWalkRate"></param>
        public static void SetWalkRateCap(NWCreature creature, float fWalkRate = 2000.0f)
        {
            string sFunc = "SetWalkRateCap";
            NWNX_PushArgumentFloat(NWNX_Creature, sFunc, fWalkRate);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Sets the creature's gold without sending a feedback message
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="gold"></param>
        public static void SetGold(NWCreature creature, int gold)
        {
            string sFunc = "SetGold";
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, gold);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Sets corpse decay time in milliseconds
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="nDecayTime"></param>
        public static void SetCorpseDecayTime(NWCreature creature, int nDecayTime)
        {
            string sFunc = "SetCorpseDecayTime";
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, nDecayTime);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Returns the creature's base save and any modifiers set in the toolset
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="which"></param>
        /// <returns></returns>
        public static int GetBaseSavingThrow(NWCreature creature, int which)
        {
            string sFunc = "GetBaseSavingThrow";
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, which);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Sets the base saving throw of the creature
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="which"></param>
        /// <param name="value"></param>
        public static void SetBaseSavingThrow(NWCreature creature, int which, int value)
        {
            string sFunc = "SetBaseSavingThrow";
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, value);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, which);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Add count levels of class to the creature, bypassing all validation
        /// This will not work on player characters
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="class"></param>
        /// <param name="count"></param>
        public static void LevelUp(NWCreature creature, int @class, int count = 1)
        {
            string sFunc = "LevelUp";
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, count);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, @class);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Remove last count levels from a creature
        /// This will not work on player characters
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="count"></param>
        public static void LevelDown(NWCreature creature, int count = 1)
        {
            string sFunc = "LevelDown";
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, count);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Sets the creature's challenge rating
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="fCR"></param>
        public static void SetChallengeRating(NWCreature creature, float fCR)
        {
            string sFunc = "SetChallengeRating";
            NWNX_PushArgumentFloat(NWNX_Creature, sFunc, fCR);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// // Returns the creature's highest attack bonus based on its own stats
        /// NOTE: AB vs. <Type> and +AB on Gauntlets are excluded
        ///
        /// int isMelee values:
        ///   TRUE: Get Melee/Unarmed Attack Bonus
        ///   FALSE: Get Ranged Attack Bonus
        ///   -1: Get Attack Bonus depending on the weapon creature has equipped in its right hand
        ///       Defaults to Melee Attack Bonus if weapon is invalid or no weapon
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="isMelee"></param>
        /// <param name="isTouchAttack"></param>
        /// <param name="isOffhand"></param>
        /// <param name="includeBaseAttackBonus"></param>
        /// <returns></returns>
        public static int GetAttackBonus(NWCreature creature, int isMelee = -1, int isTouchAttack = FALSE, int isOffhand = FALSE, int includeBaseAttackBonus = TRUE)
        {
            string sFunc = "GetAttackBonus";

            if (isMelee == -1)
            {
                NWItem oWeapon = GetItemInSlot(INVENTORY_SLOT_RIGHTHAND, creature);

                if (oWeapon.IsValid)
                {
                    isMelee = oWeapon.IsRanged ? _.FALSE : _.TRUE;
                }
                else
                {// Default to melee for unarmed
                    isMelee = TRUE;
                }
            }

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, includeBaseAttackBonus);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, isOffhand);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, isTouchAttack);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, isMelee);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Get feat remaining uses of a creature
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="feat"></param>
        /// <returns></returns>
        public static int GetFeatRemainingUses(NWCreature creature, int feat)
        {
            string sFunc = "GetFeatRemainingUses";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, feat);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Get feat total uses of a creature
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="feat"></param>
        /// <returns></returns>
        public static int GetFeatTotalUses(NWCreature creature, int feat)
        {
            string sFunc = "GetFeatTotalUses";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, feat);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Set feat remaining uses of a creature
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="feat"></param>
        /// <param name="uses"></param>
        public static void SetFeatRemainingUses(NWCreature creature, int feat, int uses)
        {
            string sFunc = "SetFeatRemainingUses";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, uses);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, feat);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Get total effect bonus
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="bonusType"></param>
        /// <param name="target"></param>
        /// <param name="isElemental"></param>
        /// <param name="isForceMax"></param>
        /// <param name="savetype"></param>
        /// <param name="saveSpecificType"></param>
        /// <param name="skill"></param>
        /// <param name="abilityScore"></param>
        /// <param name="isOffhand"></param>
        /// <returns></returns>
        public static int GetTotalEffectBonus(
            NWCreature creature, 
            CreatureBonusType bonusType = CreatureBonusType.Attack, 
            NWObject target = null, 
            int isElemental = 0,
            int isForceMax = 0, 
            int savetype = -1, 
            int saveSpecificType = -1, 
            int skill = -1, 
            int abilityScore = -1, 
            int isOffhand = FALSE)
        {
            if (target == null) target = new NWGameObject();

            string sFunc = "GetTotalEffectBonus";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, isOffhand);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, abilityScore);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, skill);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, saveSpecificType);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, savetype);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, isForceMax);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, isElemental);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, target);
            NWNX_PushArgumentInt(NWNX_Creature, sFunc, (int)bonusType);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Set the original first or last name of creature
        /// For PCs this will persist to the .bic file if saved. Requires a relog to update.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="name"></param>
        /// <param name="isLastName"></param>
        public static void SetOriginalName(NWCreature creature, string name, int isLastName)
        {
            string sFunc = "SetOriginalName";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, isLastName);
            NWNX_PushArgumentString(NWNX_Creature, sFunc, name);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Get the original first or last name of creature
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="isLastName"></param>
        /// <returns></returns>
        public static string GetOriginalName(NWCreature creature, bool isLastName)
        {
            string sFunc = "GetOriginalName";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, isLastName ? 1 : 0);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
            return NWNX_GetReturnValueString(NWNX_Creature, sFunc);
        }

        /// <summary>
        /// Set creature's spell resistance
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="sr"></param>
        public static void SetSpellResistance(NWCreature creature, int sr)
        {
            string sFunc = "SetSpellResistance";

            NWNX_PushArgumentInt(NWNX_Creature, sFunc, sr);
            NWNX_PushArgumentObject(NWNX_Creature, sFunc, creature);

            NWNX_CallFunction(NWNX_Creature, sFunc);
        }


    }
}
