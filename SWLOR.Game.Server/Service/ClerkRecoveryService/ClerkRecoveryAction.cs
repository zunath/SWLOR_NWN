namespace SWLOR.Game.Server.Service.ClerkRecoveryService
{
    /// <summary>
    /// What the City Hall clerk does for a crop or an animal its owner cannot reach (Player City Revamp, "Farming"). Clearing a
    /// crop writes nothing to overflow storage, so it needs no mark; the others are the actions a mark records.
    /// </summary>
    public enum ClerkRecoveryAction
    {
        Invalid = 0,

        /// <summary>Harvests a ripe crop. The harvest and any returned seed go to overflow storage.</summary>
        Harvest = 1,

        /// <summary>Clears the crop. Its seed is lost.</summary>
        Clear = 2,

        /// <summary>Collects from an adult. The manure and milk go to overflow storage.</summary>
        Collect = 3,

        /// <summary>Butchers an adult. The meat and second product go to overflow storage.</summary>
        Butcher = 4,

        /// <summary>Packs the animal into a crate that goes to overflow storage.</summary>
        Crate = 5
    }
}
