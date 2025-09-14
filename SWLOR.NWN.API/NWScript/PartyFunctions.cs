namespace SWLOR.NWN.API.NWScript
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
            global::NWN.Core.NWScript.AddToParty(oPC, oPartyLeader);
        }

        /// <summary>
        ///   Remove oPC from their current party. This will only work on a PC.
        ///   - oPC: removes this player from whatever party they're currently in.
        /// </summary>
        public static void RemoveFromParty(uint oPC)
        {
            global::NWN.Core.NWScript.RemoveFromParty(oPC);
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
            global::NWN.Core.NWScript.SetPanelButtonFlash(oPlayer, nButton, nEnableFlash);
        }
    }
}