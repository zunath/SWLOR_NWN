namespace SWLOR.Game.Server.Service.BeastMasteryService
{
    /// <summary>
    /// How a player can obtain a given incubation field note, in addition to always
    /// being able to discover it by performing the mutation itself.
    /// </summary>
    public enum FieldNoteAcquisitionType
    {
        Invalid = 0,

        // Not sold. Dropped by rare spawn elites (and by discovery).
        RareSpawnDrop = 1,

        // Not sold. Dropped by bosses and other non-rare-spawn creatures (and by discovery).
        BossDrop = 2,

        // Sold by the field note vendor, and may also drop (and by discovery).
        Store = 3,
    }
}
