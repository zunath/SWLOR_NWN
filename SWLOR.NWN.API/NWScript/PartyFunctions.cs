namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Adds the PC to the party leader's party. This will only work on two PCs.
        /// </summary>
        /// <param name="oPC">Player to add to a party</param>
        /// <param name="oPartyLeader">Player already in the party</param>
        public static void AddToParty(uint oPC, uint oPartyLeader)
        {
            global::NWN.Core.NWScript.AddToParty(oPC, oPartyLeader);
        }

        /// <summary>
        /// Removes the PC from their current party. This will only work on a PC.
        /// </summary>
        /// <param name="oPC">Removes this player from whatever party they're currently in</param>
        public static void RemoveFromParty(uint oPC)
        {
            global::NWN.Core.NWScript.RemoveFromParty(oPC);
        }

        /// <summary>
        /// Makes the corresponding panel button on the player's client start or stop flashing.
        /// </summary>
        /// <param name="oPlayer">The player</param>
        /// <param name="nButton">PANEL_BUTTON_* constant</param>
        /// <param name="nEnableFlash">If TRUE, the button will start flashing. If FALSE, the button will stop flashing</param>
        public static void SetPanelButtonFlash(uint oPlayer, int nButton, int nEnableFlash)
        {
            global::NWN.Core.NWScript.SetPanelButtonFlash(oPlayer, nButton, nEnableFlash);
        }
    }
}