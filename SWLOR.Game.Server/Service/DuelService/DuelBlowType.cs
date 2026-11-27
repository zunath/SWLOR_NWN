namespace SWLOR.Game.Server.Service.DuelService
{
    /// <summary>
    /// What a blow on a duelist is, judged by who dealt it once pets, droids and damaging effects are traced back to
    /// their owner or creator.
    /// </summary>
    public enum DuelBlowType
    {
        /// <summary>Dealt by one of the two duelists, or by their pets and effects.</summary>
        Duel = 1,

        /// <summary>Dealt by anyone else. It voids the duel.</summary>
        Interference = 2,

        /// <summary>No source could be found, such as a hazard. It neither continues nor voids the duel.</summary>
        Unattributed = 3
    }
}
