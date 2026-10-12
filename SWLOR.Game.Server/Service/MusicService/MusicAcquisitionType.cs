namespace SWLOR.Game.Server.Service.MusicService
{
    /// <summary>
    /// How a player obtains a song for their music collection.
    /// </summary>
    public enum MusicAcquisitionType
    {
        Invalid = 0,

        // Granted to every character.
        Starter = 1,

        // Sold as a song disc in a store. Never dropped.
        Store = 2,

        // Dropped by regular enemies from the music disc loot table.
        Loot = 3,

        // Dropped only by bosses from the boss music disc loot table.
        BossDrop = 4,
    }
}
