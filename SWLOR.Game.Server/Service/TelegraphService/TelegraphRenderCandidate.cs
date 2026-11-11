namespace SWLOR.Game.Server.Service.TelegraphService
{
    /// <summary>
    /// One active telegraph as a specific viewer would see it. Relationship state varies per viewer,
    /// so it is resolved once per candidate before slots are assigned.
    /// </summary>
    public readonly record struct TelegraphRenderCandidate(
        ActiveTelegraph Telegraph,
        TelegraphColorType ColorType,
        bool CreatorIsEnemy)
    {
        /// <summary>A shape an enemy will hit or has placed, which the viewer may need to react to.</summary>
        public bool IsEnemyThreat => CreatorIsEnemy && Telegraph.Data.IsHostile;

        /// <summary>
        /// An impact flash marks an area that has already been struck, so allied flashes give the
        /// viewer nothing to act on. Warnings and lingering areas are always eligible.
        /// </summary>
        public bool IsVisible => !Telegraph.Data.IsImpactFlash ||
                                 ColorType == TelegraphColorType.Self ||
                                 CreatorIsEnemy;
    }
}
