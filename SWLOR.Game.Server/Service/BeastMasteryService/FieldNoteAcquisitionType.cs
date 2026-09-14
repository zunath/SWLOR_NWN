namespace SWLOR.Game.Server.Service.BeastMasteryService
{
    /// <summary>
    /// How a player can obtain a given incubation field note, in addition to always
    /// being able to discover it by performing the mutation itself.
    /// </summary>
    public enum FieldNoteAcquisitionType
    {
        Invalid = 0,

        // Only obtainable by actually performing the mutation. Never sold or dropped.
        DiscoveryOnly = 1,

        // Obtainable from boss loot tables (and by discovery). Never sold in a store.
        BossDrop = 2,

        // Obtainable from special stores and loot (and by discovery).
        Store = 3,
    }
}
