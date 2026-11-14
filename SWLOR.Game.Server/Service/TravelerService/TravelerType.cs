namespace SWLOR.Game.Server.Service.TravelerService
{
    /// <summary>
    /// The traveling NPCs (Player City Revamp, "Travelers"). Each visits a different player city every week. Values are
    /// saved with the traveler's state, and the member name is the key of its stock allowance records, so neither changes.
    /// </summary>
    public enum TravelerType
    {
        Invalid = 0,

        /// <summary>Sells young animals for the corrals.</summary>
        BeastTrader = 1,

        /// <summary>Sells a rotating few tier 3 to 5 seeds and rare materials.</summary>
        Prospector = 2,

        /// <summary>Sells outfits.</summary>
        FashionMerchant = 3,

        /// <summary>Sells nothing: tells rumors, says where the other travelers are, and opens the contract board.</summary>
        InformationBroker = 4
    }
}
