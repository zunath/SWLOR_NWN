namespace SWLOR.Game.Server.Service.SpawnService
{
    /// <summary>
    /// Which of a player city's spawn rules a spawn follows.
    /// </summary>
    public enum SpawnExclusionClass
    {
        /// <summary>
        /// Everything else: friendly NPCs, terminals, fishing points and the like. Never excluded or moved.
        /// </summary>
        Other = 0,

        /// <summary>
        /// A creature that is hostile to players. Keeps the building buffer and stays out of the Settled zone.
        /// </summary>
        Hostile = 1,

        /// <summary>
        /// A harvest or scavenge placeable. Keeps the building buffer; the Settled zone is for hostiles only.
        /// </summary>
        ResourceNode = 2,
    }
}
