namespace SWLOR.Game.Server.Core.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Add oPC to oPartyLeader's party.  This will only work on two PCs.
        ///   - oPC: player to add to a party
        ///   - oPartyLeader: player already in the party
        /// </summary>
        public static void AddToParty(uint oPC, uint oPartyLeader)
        {
            VM.StackPush(oPartyLeader);
            VM.StackPush(oPC);
            VM.Call(572);
        }

        /// <summary>
        ///   Remove oPC from their current party. This will only work on a PC.
        ///   - oPC: removes this player from whatever party they're currently in.
        /// </summary>
        public static void RemoveFromParty(uint oPC)
        {
            VM.StackPush(oPC);
            VM.Call(573);
        }

        /// <summary>
        ///   Make the corresponding panel button on the player's client start or stop
        ///   flashing.
        ///   - oPlayer
        ///   - nButton: PANEL_BUTTON_*
        ///   - nEnableFlash: if this is TRUE nButton will start flashing.  It if is FALSE,
        ///   nButton will stop flashing.
        /// </summary>
        public static void SetPanelButtonFlash(uint oPlayer, int nButton, int nEnableFlash)
        {
            VM.StackPush(nEnableFlash);
            VM.StackPush(nButton);
            VM.StackPush(oPlayer);
            VM.Call(521);
        }
    }
}