namespace SWLOR.Game.Server.Core.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Add a journal quest entry to oCreature.
        ///   - szPlotID: the plot identifier used in the toolset's Journal Editor
        ///   - nState: the state of the plot as seen in the toolset's Journal Editor
        ///   - oCreature
        ///   - bAllPartyMembers: If this is TRUE, the entry will show up in the journal of
        ///   everyone in the party
        ///   - bAllPlayers: If this is TRUE, the entry will show up in the journal of
        ///   everyone in the world
        ///   - bAllowOverrideHigher: If this is TRUE, you can set the state to a lower
        ///   number than the one it is currently on
        /// </summary>
        public static void AddJournalQuestEntry(string szPlotID, int nState, uint oCreature,
            bool bAllPartyMembers = true, bool bAllPlayers = false, bool bAllowOverrideHigher = false)
        {
            Internal.NativeFunctions.StackPushInteger(bAllowOverrideHigher ? 1 : 0);
            Internal.NativeFunctions.StackPushInteger(bAllPlayers ? 1 : 0);
            Internal.NativeFunctions.StackPushInteger(bAllPartyMembers ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oCreature);
            Internal.NativeFunctions.StackPushInteger(nState);
            Internal.NativeFunctions.StackPushStringUTF8(szPlotID);
            Internal.NativeFunctions.CallBuiltIn(367);
        }

        /// <summary>
        ///   Remove a journal quest entry from oCreature.
        ///   - szPlotID: the plot identifier used in the toolset's Journal Editor
        ///   - oCreature
        ///   - bAllPartyMembers: If this is TRUE, the entry will be removed from the
        ///   journal of everyone in the party
        ///   - bAllPlayers: If this is TRUE, the entry will be removed from the journal of
        ///   everyone in the world
        /// </summary>
        public static void RemoveJournalQuestEntry(string szPlotID, uint oCreature, bool bAllPartyMembers = true,
            bool bAllPlayers = false)
        {
            Internal.NativeFunctions.StackPushInteger(bAllPlayers ? 1 : 0);
            Internal.NativeFunctions.StackPushInteger(bAllPartyMembers ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oCreature);
            Internal.NativeFunctions.StackPushStringUTF8(szPlotID);
            Internal.NativeFunctions.CallBuiltIn(368);
        }
    }
}