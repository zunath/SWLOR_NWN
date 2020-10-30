using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Core.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Get the weakest member of oFactionMember's faction.
        ///   * Returns OBJECT_INVALID if oFactionMember's faction is invalid.
        /// </summary>
        public static uint GetFactionWeakestMember(uint oFactionMember = OBJECT_INVALID, bool bMustBeVisible = true)
        {
            Internal.NativeFunctions.StackPushInteger(bMustBeVisible ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oFactionMember);
            Internal.NativeFunctions.CallBuiltIn(181);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Get the strongest member of oFactionMember's faction.
        ///   * Returns OBJECT_INVALID if oFactionMember's faction is invalid.
        /// </summary>
        public static uint GetFactionStrongestMember(uint oFactionMember = OBJECT_INVALID, bool bMustBeVisible = true)
        {
            Internal.NativeFunctions.StackPushInteger(bMustBeVisible ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oFactionMember);
            Internal.NativeFunctions.CallBuiltIn(182);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Get the member of oFactionMember's faction that has taken the most hit points
        ///   of damage.
        ///   * Returns OBJECT_INVALID if oFactionMember's faction is invalid.
        /// </summary>
        public static uint GetFactionMostDamagedMember(uint oFactionMember = OBJECT_INVALID, bool bMustBeVisible = true)
        {
            Internal.NativeFunctions.StackPushInteger(bMustBeVisible ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oFactionMember);
            Internal.NativeFunctions.CallBuiltIn(183);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Get the member of oFactionMember's faction that has taken the fewest hit
        ///   points of damage.
        ///   * Returns OBJECT_INVALID if oFactionMember's faction is invalid.
        /// </summary>
        public static uint GetFactionLeastDamagedMember(uint oFactionMember = OBJECT_INVALID,
            bool bMustBeVisible = true)
        {
            Internal.NativeFunctions.StackPushInteger(bMustBeVisible ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oFactionMember);
            Internal.NativeFunctions.CallBuiltIn(184);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Get the amount of gold held by oFactionMember's faction.
        ///   * Returns -1 if oFactionMember's faction is invalid.
        /// </summary>
        public static int GetFactionGold(uint oFactionMember)
        {
            Internal.NativeFunctions.StackPushObject(oFactionMember);
            Internal.NativeFunctions.CallBuiltIn(185);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get an integer between 0 and 100 (inclusive) that represents how
        ///   oSourceFactionMember's faction feels about oTarget.
        ///   * Return value on error: -1
        /// </summary>
        public static int GetFactionAverageReputation(uint oSourceFactionMember, uint oTarget)
        {
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.StackPushObject(oSourceFactionMember);
            Internal.NativeFunctions.CallBuiltIn(186);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get an integer between 0 and 100 (inclusive) that represents the average
        ///   good/evil alignment of oFactionMember's faction.
        ///   * Return value on error: -1
        /// </summary>
        public static int GetFactionAverageGoodEvilAlignment(uint oFactionMember)
        {
            Internal.NativeFunctions.StackPushObject(oFactionMember);
            Internal.NativeFunctions.CallBuiltIn(187);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get an integer between 0 and 100 (inclusive) that represents the average
        ///   law/chaos alignment of oFactionMember's faction.
        ///   * Return value on error: -1
        /// </summary>
        public static int GetFactionAverageLawChaosAlignment(uint oFactionMember)
        {
            Internal.NativeFunctions.StackPushObject(oFactionMember);
            Internal.NativeFunctions.CallBuiltIn(188);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the average level of the members of the faction.
        ///   * Return value on error: -1
        /// </summary>
        public static int GetFactionAverageLevel(uint oFactionMember)
        {
            Internal.NativeFunctions.StackPushObject(oFactionMember);
            Internal.NativeFunctions.CallBuiltIn(189);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the average XP of the members of the faction.
        ///   * Return value on error: -1
        /// </summary>
        public static int GetFactionAverageXP(uint oFactionMember)
        {
            Internal.NativeFunctions.StackPushObject(oFactionMember);
            Internal.NativeFunctions.CallBuiltIn(190);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the most frequent class in the faction - this can be compared with the
        ///   constants CLASS_TYPE_*.
        ///   * Return value on error: -1
        /// </summary>
        public static int GetFactionMostFrequentClass(uint oFactionMember)
        {
            Internal.NativeFunctions.StackPushObject(oFactionMember);
            Internal.NativeFunctions.CallBuiltIn(191);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the object faction member with the lowest armour class.
        ///   * Returns OBJECT_INVALID if oFactionMember's faction is invalid.
        /// </summary>
        public static uint GetFactionWorstAC(uint oFactionMember = OBJECT_INVALID, bool bMustBeVisible = true)
        {
            Internal.NativeFunctions.StackPushInteger(bMustBeVisible ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oFactionMember);
            Internal.NativeFunctions.CallBuiltIn(192);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Get the object faction member with the highest armour class.
        ///   * Returns OBJECT_INVALID if oFactionMember's faction is invalid.
        /// </summary>
        public static uint GetFactionBestAC(uint oFactionMember = OBJECT_INVALID, bool bMustBeVisible = true)
        {
            Internal.NativeFunctions.StackPushInteger(bMustBeVisible ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oFactionMember);
            Internal.NativeFunctions.CallBuiltIn(193);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Get an integer between 0 and 100 (inclusive) that represents how oSource
        ///   feels about oTarget.
        ///   -> 0-10 means oSource is hostile to oTarget
        ///   -> 11-89 means oSource is neutral to oTarget
        ///   -> 90-100 means oSource is friendly to oTarget
        ///   * Returns -1 if oSource or oTarget does not identify a valid object
        /// </summary>
        public static int GetReputation(uint oSource, uint oTarget)
        {
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.StackPushObject(oSource);
            Internal.NativeFunctions.CallBuiltIn(208);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Adjust how oSourceFactionMember's faction feels about oTarget by the
        ///   specified amount.
        ///   Note: This adjusts Faction Reputation, how the entire faction that
        ///   oSourceFactionMember is in, feels about oTarget.
        ///   * No return value
        ///   Note: You can't adjust a player character's (PC) faction towards
        ///   NPCs, so attempting to make an NPC hostile by passing in a PC object
        ///   as oSourceFactionMember in the following call will fail:
        ///   AdjustReputation(oNPC,oPC,-100);
        ///   Instead you should pass in the PC object as the first
        ///   parameter as in the following call which should succeed:
        ///   AdjustReputation(oPC,oNPC,-100);
        ///   Note: Will fail if oSourceFactionMember is a plot object.
        /// </summary>
        public static void AdjustReputation(uint oTarget, uint oSourceFactionMember, int nAdjustment)
        {
            Internal.NativeFunctions.StackPushInteger(nAdjustment);
            Internal.NativeFunctions.StackPushObject(oSourceFactionMember);
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.CallBuiltIn(209);
        }

        /// <summary>
        ///   * Returns TRUE if oSource considers oTarget as an enemy.
        /// </summary>
        public static bool GetIsEnemy(uint oTarget, uint oSource = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oSource);
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.CallBuiltIn(235);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   * Returns TRUE if oSource considers oTarget as a friend.
        /// </summary>
        public static bool GetIsFriend(uint oTarget, uint oSource = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oSource);
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.CallBuiltIn(236);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   * Returns TRUE if oSource considers oTarget as neutral.
        /// </summary>
        public static bool GetIsNeutral(uint oTarget, uint oSource = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oSource);
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.CallBuiltIn(237);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   Get the player leader of the faction of which oMemberOfFaction is a member.
        ///   * Returns OBJECT_INVALID if oMemberOfFaction is not a valid creature,
        ///   or oMemberOfFaction is a member of a NPC faction.
        /// </summary>
        public static uint GetFactionLeader(uint oMemberOfFaction)
        {
            Internal.NativeFunctions.StackPushObject(oMemberOfFaction);
            Internal.NativeFunctions.CallBuiltIn(562);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Set how nStandardFaction feels about oCreature.
        ///   - nStandardFaction: STANDARD_FACTION_*
        ///   - nNewReputation: 0-100 (inclusive)
        ///   - oCreature
        /// </summary>
        public static void SetStandardFactionReputation(StandardFaction nStandardFaction, int nNewReputation,
            uint oCreature = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oCreature);
            Internal.NativeFunctions.StackPushInteger(nNewReputation);
            Internal.NativeFunctions.StackPushInteger((int)nStandardFaction);
            Internal.NativeFunctions.CallBuiltIn(523);
        }

        /// <summary>
        ///   Find out how nStandardFaction feels about oCreature.
        ///   - nStandardFaction: STANDARD_FACTION_*
        ///   - oCreature
        ///   Returns -1 on an error.
        ///   Returns 0-100 based on the standing of oCreature within the faction nStandardFaction.
        ///   0-10   :  Hostile.
        ///   11-89  :  Neutral.
        ///   90-100 :  Friendly.
        /// </summary>
        public static int GetStandardFactionReputation(StandardFaction nStandardFaction, uint oCreature = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oCreature);
            Internal.NativeFunctions.StackPushInteger((int)nStandardFaction);
            Internal.NativeFunctions.CallBuiltIn(524);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Make oCreatureToChange join one of the standard factions.
        ///   ** This will only work on an NPC **
        ///   - nStandardFaction: STANDARD_FACTION_*
        /// </summary>
        public static void ChangeToStandardFaction(uint oCreatureToChange, StandardFaction nStandardFaction)
        {
            Internal.NativeFunctions.StackPushInteger((int)nStandardFaction);
            Internal.NativeFunctions.StackPushObject(oCreatureToChange);
            Internal.NativeFunctions.CallBuiltIn(412);
        }

        /// <summary>
        ///   Get the first member of oMemberOfFaction's faction (start to cycle through
        ///   oMemberOfFaction's faction).
        ///   * Returns OBJECT_INVALID if oMemberOfFaction's faction is invalid.
        /// </summary>
        public static uint GetFirstFactionMember(uint oMemberOfFaction, bool bPCOnly = true)
        {
            Internal.NativeFunctions.StackPushInteger(bPCOnly ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oMemberOfFaction);
            Internal.NativeFunctions.CallBuiltIn(380);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Get the next member of oMemberOfFaction's faction (continue to cycle through
        ///   oMemberOfFaction's faction).
        ///   * Returns OBJECT_INVALID if oMemberOfFaction's faction is invalid.
        /// </summary>
        public static uint GetNextFactionMember(uint oMemberOfFaction, bool bPCOnly = true)
        {
            Internal.NativeFunctions.StackPushInteger(bPCOnly ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oMemberOfFaction);
            Internal.NativeFunctions.CallBuiltIn(381);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   * Returns TRUE if the Faction Ids of the two objects are the same
        /// </summary>
        public static bool GetFactionEqual(uint oFirstObject, uint oSecondObject = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oSecondObject);
            Internal.NativeFunctions.StackPushObject(oFirstObject);
            Internal.NativeFunctions.CallBuiltIn(172);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   Make oObjectToChangeFaction join the faction of oMemberOfFactionToJoin.
        ///   NB. ** This will only work for two NPCs **
        /// </summary>
        public static void ChangeFaction(uint oObjectToChangeFaction, uint oMemberOfFactionToJoin)
        {
            Internal.NativeFunctions.StackPushObject(oMemberOfFactionToJoin);
            Internal.NativeFunctions.StackPushObject(oObjectToChangeFaction);
            Internal.NativeFunctions.CallBuiltIn(173);
        }
    }
}