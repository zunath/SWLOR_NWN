using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Gets the weakest member of the faction member's faction.
        /// </summary>
        /// <param name="oFactionMember">The faction member to check (default: OBJECT_INVALID)</param>
        /// <param name="bMustBeVisible">Whether the member must be visible (default: true)</param>
        /// <returns>The weakest faction member. Returns OBJECT_INVALID if the faction member's faction is invalid</returns>
        public static uint GetFactionWeakestMember(uint oFactionMember = OBJECT_INVALID, bool bMustBeVisible = true)
        {
            return global::NWN.Core.NWScript.GetFactionWeakestMember(oFactionMember, bMustBeVisible ? 1 : 0);
        }

        /// <summary>
        /// Gets the strongest member of the faction member's faction.
        /// </summary>
        /// <param name="oFactionMember">The faction member to check (default: OBJECT_INVALID)</param>
        /// <param name="bMustBeVisible">Whether the member must be visible (default: true)</param>
        /// <returns>The strongest faction member. Returns OBJECT_INVALID if the faction member's faction is invalid</returns>
        public static uint GetFactionStrongestMember(uint oFactionMember = OBJECT_INVALID, bool bMustBeVisible = true)
        {
            return global::NWN.Core.NWScript.GetFactionStrongestMember(oFactionMember, bMustBeVisible ? 1 : 0);
        }

        /// <summary>
        /// Gets the member of the faction member's faction that has taken the most hit points of damage.
        /// </summary>
        /// <param name="oFactionMember">The faction member to check (default: OBJECT_INVALID)</param>
        /// <param name="bMustBeVisible">Whether the member must be visible (default: true)</param>
        /// <returns>The most damaged faction member. Returns OBJECT_INVALID if the faction member's faction is invalid</returns>
        public static uint GetFactionMostDamagedMember(uint oFactionMember = OBJECT_INVALID, bool bMustBeVisible = true)
        {
            return global::NWN.Core.NWScript.GetFactionMostDamagedMember(oFactionMember, bMustBeVisible ? 1 : 0);
        }

        /// <summary>
        /// Gets the member of the faction member's faction that has taken the fewest hit points of damage.
        /// </summary>
        /// <param name="oFactionMember">The faction member to check (default: OBJECT_INVALID)</param>
        /// <param name="bMustBeVisible">Whether the member must be visible (default: true)</param>
        /// <returns>The least damaged faction member. Returns OBJECT_INVALID if the faction member's faction is invalid</returns>
        public static uint GetFactionLeastDamagedMember(uint oFactionMember = OBJECT_INVALID,
            bool bMustBeVisible = true)
        {
            return global::NWN.Core.NWScript.GetFactionLeastDamagedMember(oFactionMember, bMustBeVisible ? 1 : 0);
        }

        /// <summary>
        /// Gets the amount of gold held by the faction member's faction.
        /// </summary>
        /// <param name="oFactionMember">The faction member to check</param>
        /// <returns>The amount of gold held by the faction. Returns -1 if the faction member's faction is invalid</returns>
        public static int GetFactionGold(uint oFactionMember)
        {
            return global::NWN.Core.NWScript.GetFactionGold(oFactionMember);
        }

        /// <summary>
        /// Gets an integer between 0 and 100 (inclusive) that represents how the source faction member's faction feels about the target.
        /// </summary>
        /// <param name="oSourceFactionMember">The source faction member</param>
        /// <param name="oTarget">The target to check reputation for</param>
        /// <returns>An integer between 0 and 100 representing the reputation. Returns -1 on error</returns>
        public static int GetFactionAverageReputation(uint oSourceFactionMember, uint oTarget)
        {
            return global::NWN.Core.NWScript.GetFactionAverageReputation(oSourceFactionMember, oTarget);
        }

        /// <summary>
        /// Gets an integer between 0 and 100 (inclusive) that represents the average good/evil alignment of the faction member's faction.
        /// </summary>
        /// <param name="oFactionMember">The faction member to check</param>
        /// <returns>An integer between 0 and 100 representing the average good/evil alignment. Returns -1 on error</returns>
        public static int GetFactionAverageGoodEvilAlignment(uint oFactionMember)
        {
            return global::NWN.Core.NWScript.GetFactionAverageGoodEvilAlignment(oFactionMember);
        }

        /// <summary>
        /// Gets an integer between 0 and 100 (inclusive) that represents the average law/chaos alignment of the faction member's faction.
        /// </summary>
        /// <param name="oFactionMember">The faction member to check</param>
        /// <returns>An integer between 0 and 100 representing the average law/chaos alignment. Returns -1 on error</returns>
        public static int GetFactionAverageLawChaosAlignment(uint oFactionMember)
        {
            return global::NWN.Core.NWScript.GetFactionAverageLawChaosAlignment(oFactionMember);
        }

        /// <summary>
        /// Gets the average level of the members of the faction.
        /// </summary>
        /// <param name="oFactionMember">The faction member to check</param>
        /// <returns>The average level of the faction members. Returns -1 on error</returns>
        public static int GetFactionAverageLevel(uint oFactionMember)
        {
            return global::NWN.Core.NWScript.GetFactionAverageLevel(oFactionMember);
        }

        /// <summary>
        /// Gets the average XP of the members of the faction.
        /// </summary>
        /// <param name="oFactionMember">The faction member to check</param>
        /// <returns>The average XP of the faction members. Returns -1 on error</returns>
        public static int GetFactionAverageXP(uint oFactionMember)
        {
            return global::NWN.Core.NWScript.GetFactionAverageXP(oFactionMember);
        }

        /// <summary>
        /// Gets the most frequent class in the faction.
        /// </summary>
        /// <param name="oFactionMember">The faction member to check</param>
        /// <returns>The most frequent class in the faction (can be compared with CLASS_TYPE_* constants). Returns -1 on error</returns>
        public static int GetFactionMostFrequentClass(uint oFactionMember)
        {
            return global::NWN.Core.NWScript.GetFactionMostFrequentClass(oFactionMember);
        }

        /// <summary>
        /// Gets the faction member with the lowest armor class.
        /// </summary>
        /// <param name="oFactionMember">The faction member to check (default: OBJECT_INVALID)</param>
        /// <param name="bMustBeVisible">Whether the member must be visible (default: true)</param>
        /// <returns>The faction member with the worst AC. Returns OBJECT_INVALID if the faction member's faction is invalid</returns>
        public static uint GetFactionWorstAC(uint oFactionMember = OBJECT_INVALID, bool bMustBeVisible = true)
        {
            return global::NWN.Core.NWScript.GetFactionWorstAC(oFactionMember, bMustBeVisible ? 1 : 0);
        }

        /// <summary>
        /// Gets the faction member with the highest armor class.
        /// </summary>
        /// <param name="oFactionMember">The faction member to check (default: OBJECT_INVALID)</param>
        /// <param name="bMustBeVisible">Whether the member must be visible (default: true)</param>
        /// <returns>The faction member with the best AC. Returns OBJECT_INVALID if the faction member's faction is invalid</returns>
        public static uint GetFactionBestAC(uint oFactionMember = OBJECT_INVALID, bool bMustBeVisible = true)
        {
            return global::NWN.Core.NWScript.GetFactionBestAC(oFactionMember, bMustBeVisible ? 1 : 0);
        }

        /// <summary>
        /// Gets an integer between 0 and 100 (inclusive) that represents how the source feels about the target.
        /// </summary>
        /// <param name="oSource">The source object</param>
        /// <param name="oTarget">The target object</param>
        /// <returns>An integer between 0 and 100 representing the reputation. 0-10 means hostile, 11-89 means neutral, 90-100 means friendly. Returns -1 if oSource or oTarget does not identify a valid object</returns>
        public static int GetReputation(uint oSource, uint oTarget)
        {
            return global::NWN.Core.NWScript.GetReputation(oSource, oTarget);
        }

        /// <summary>
        /// Adjusts how the source faction member's faction feels about the target by the specified amount.
        /// </summary>
        /// <param name="oTarget">The target to adjust reputation for</param>
        /// <param name="oSourceFactionMember">The source faction member</param>
        /// <param name="nAdjustment">The amount to adjust the reputation by</param>
        /// <remarks>This adjusts Faction Reputation, how the entire faction that oSourceFactionMember is in, feels about oTarget. You can't adjust a player character's (PC) faction towards NPCs, so attempting to make an NPC hostile by passing in a PC object as oSourceFactionMember in the following call will fail: AdjustReputation(oNPC,oPC,-100); Instead you should pass in the PC object as the first parameter as in the following call which should succeed: AdjustReputation(oPC,oNPC,-100); Will fail if oSourceFactionMember is a plot object.</remarks>
        public static void AdjustReputation(uint oTarget, uint oSourceFactionMember, int nAdjustment)
        {
            global::NWN.Core.NWScript.AdjustReputation(oTarget, oSourceFactionMember, nAdjustment);
        }

        /// <summary>
        /// Returns true if the source considers the target as an enemy.
        /// </summary>
        /// <param name="oTarget">The target to check</param>
        /// <param name="oSource">The source to check from (default: OBJECT_INVALID)</param>
        /// <returns>True if the source considers the target as an enemy, false otherwise</returns>
        public static bool GetIsEnemy(uint oTarget, uint oSource = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetIsEnemy(oTarget, oSource) != 0;
        }

        /// <summary>
        /// Returns true if the source considers the target as a friend.
        /// </summary>
        /// <param name="oTarget">The target to check</param>
        /// <param name="oSource">The source to check from (default: OBJECT_INVALID)</param>
        /// <returns>True if the source considers the target as a friend, false otherwise</returns>
        public static bool GetIsFriend(uint oTarget, uint oSource = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetIsFriend(oTarget, oSource) != 0;
        }

        /// <summary>
        /// Returns true if the source considers the target as neutral.
        /// </summary>
        /// <param name="oTarget">The target to check</param>
        /// <param name="oSource">The source to check from (default: OBJECT_INVALID)</param>
        /// <returns>True if the source considers the target as neutral, false otherwise</returns>
        public static bool GetIsNeutral(uint oTarget, uint oSource = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetIsNeutral(oTarget, oSource) != 0;
        }

        /// <summary>
        /// Gets the player leader of the faction of which the member is a member.
        /// </summary>
        /// <param name="oMemberOfFaction">The faction member to check</param>
        /// <returns>The faction leader. Returns OBJECT_INVALID if the member is not a valid creature, or the member is a member of an NPC faction</returns>
        public static uint GetFactionLeader(uint oMemberOfFaction)
        {
            return global::NWN.Core.NWScript.GetFactionLeader(oMemberOfFaction);
        }

        /// <summary>
        /// Sets how the standard faction feels about the specified creature.
        /// </summary>
        /// <param name="nStandardFaction">The standard faction (STANDARD_FACTION_* constants)</param>
        /// <param name="nNewReputation">The new reputation (0-100 inclusive)</param>
        /// <param name="oCreature">The creature to set the reputation for (default: OBJECT_INVALID)</param>
        public static void SetStandardFactionReputation(StandardFaction nStandardFaction, int nNewReputation,
            uint oCreature = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetStandardFactionReputation((int)nStandardFaction, nNewReputation, oCreature);
        }

        /// <summary>
        /// Finds out how the standard faction feels about the specified creature.
        /// </summary>
        /// <param name="nStandardFaction">The standard faction (STANDARD_FACTION_* constants)</param>
        /// <param name="oCreature">The creature to check the reputation for (default: OBJECT_INVALID)</param>
        /// <returns>Returns -1 on an error. Returns 0-100 based on the standing of the creature within the faction. 0-10: Hostile, 11-89: Neutral, 90-100: Friendly</returns>
        public static int GetStandardFactionReputation(StandardFaction nStandardFaction, uint oCreature = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetStandardFactionReputation((int)nStandardFaction, oCreature);
        }

        /// <summary>
        /// Makes the creature join one of the standard factions.
        /// </summary>
        /// <param name="oCreatureToChange">The creature to change the faction for</param>
        /// <param name="nStandardFaction">The standard faction to join (STANDARD_FACTION_* constants)</param>
        /// <remarks>This will only work on an NPC.</remarks>
        public static void ChangeToStandardFaction(uint oCreatureToChange, StandardFaction nStandardFaction)
        {
            global::NWN.Core.NWScript.ChangeToStandardFaction(oCreatureToChange, (int)nStandardFaction);
        }

        /// <summary>
        /// Gets the first member of the faction member's faction.
        /// </summary>
        /// <param name="oMemberOfFaction">The faction member to get the first member for</param>
        /// <param name="bPCOnly">Whether to only return PC members (default: true)</param>
        /// <returns>The first faction member. Returns OBJECT_INVALID if the faction member's faction is invalid</returns>
        public static uint GetFirstFactionMember(uint oMemberOfFaction, bool bPCOnly = true)
        {
            return global::NWN.Core.NWScript.GetFirstFactionMember(oMemberOfFaction, bPCOnly ? 1 : 0);
        }

        /// <summary>
        /// Gets the next member of the faction member's faction.
        /// </summary>
        /// <param name="oMemberOfFaction">The faction member to get the next member for</param>
        /// <param name="bPCOnly">Whether to only return PC members (default: true)</param>
        /// <returns>The next faction member. Returns OBJECT_INVALID if the faction member's faction is invalid</returns>
        public static uint GetNextFactionMember(uint oMemberOfFaction, bool bPCOnly = true)
        {
            return global::NWN.Core.NWScript.GetNextFactionMember(oMemberOfFaction, bPCOnly ? 1 : 0);
        }

        /// <summary>
        /// Returns true if the faction IDs of the two objects are the same.
        /// </summary>
        /// <param name="oFirstObject">The first object to compare</param>
        /// <param name="oSecondObject">The second object to compare (default: OBJECT_INVALID)</param>
        /// <returns>True if the faction IDs are the same, false otherwise</returns>
        public static bool GetFactionEqual(uint oFirstObject, uint oSecondObject = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetFactionEqual(oFirstObject, oSecondObject) != 0;
        }

        /// <summary>
        /// Makes the object join the faction of the specified faction member.
        /// </summary>
        /// <param name="oObjectToChangeFaction">The object to change the faction for</param>
        /// <param name="oMemberOfFactionToJoin">The faction member whose faction to join</param>
        /// <remarks>This will only work for two NPCs.</remarks>
        public static void ChangeFaction(uint oObjectToChangeFaction, uint oMemberOfFactionToJoin)
        {
            global::NWN.Core.NWScript.ChangeFaction(oObjectToChangeFaction, oMemberOfFactionToJoin);
        }
    }
}