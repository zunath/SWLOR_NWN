namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The venom a weapon coating deals by tier. Pure functions over constants with no engine, database or service
    /// call, so item and recipe definitions (which build before and outside the service container, in the module
    /// editor tools and in offline tests) call them directly; <c>Poisons</c> forwards to them too.
    /// </summary>
    public static class PoisonsCoatingTiers
    {
        public const int LowestTier = 1;
        public const int HighestTier = 5;

        /// <summary>Venom damage per tick a coating deals by tier, before the applier's Poison Bonus.</summary>
        public static int GetVenomDamagePerTick(int tier) => System.Math.Clamp(tier, LowestTier, HighestTier) switch
        {
            1 => 8,
            2 => 12,
            3 => 16,
            4 => 21,
            _ => 26
        };

        public static float GetVenomDurationSeconds(int tier)
        {
            return 6 + System.Math.Clamp(tier, LowestTier, HighestTier) * 6;
        }
    }
}
