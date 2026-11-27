namespace SWLOR.Game.Server.Service.TownsfolkService
{
    /// <summary>
    /// The three kinds of street life a city gets (Player City Revamp, "Ambient townsfolk"). Building staff, stall vendors,
    /// and travelers are not street life: they stay at their posts.
    /// </summary>
    public enum TownsfolkGroup
    {
        Invalid = 0,

        /// <summary>Townsfolk who wander between the city's building entrances. From city level 2.</summary>
        Wanderer = 1,

        /// <summary>Guards who patrol between the entrances. Scenery only: they never fight. From city level 3.</summary>
        Guard = 2,

        /// <summary>Harmless animals roaming the streets. From city level 2, planet cities only.</summary>
        Critter = 3
    }
}
