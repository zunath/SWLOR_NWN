namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Radii of space area abilities and command range (plan section 6, shared rules for every space style).
    /// </summary>
    public static class SpaceRadii
    {
        /// <summary>
        /// Ground area radii are multiplied by this for space. Ground area abilities reach 5 m (Explosive Toss,
        /// Point Blank Burst, Concussive Toss). Space weapons reach 40 m, so 4x gives a 20 m burst: half a weapon's
        /// range, wide enough to catch a cluster of ships and small enough that one blast cannot cover a whole fight.
        /// </summary>
        public const float GroundToSpaceFactor = 4f;

        /// <summary>The ground radius most area abilities use.</summary>
        public const float GroundAreaMeters = 5f;

        /// <summary>"Enemies near your ship": 5 m x 4.</summary>
        public const float NearShip = GroundAreaMeters * GroundToSpaceFactor;

        /// <summary>"Enemies near the target": 5 m x 4.</summary>
        public const float NearTarget = GroundAreaMeters * GroundToSpaceFactor;

        /// <summary>
        /// Command range before bonuses (plan: 30 m; ground Leadership auras reach 5 m). Orders, Formation and
        /// repairs to allies reach party ships this far from the commander's ship.
        /// </summary>
        public const float CommandRange = 30f;

        /// <summary>How often a space aura looks for party ships entering and leaving its range, in seconds.</summary>
        public const double AuraRefreshSeconds = 1.0;

        /// <summary>A ground radius or bonus in meters, as space meters.</summary>
        public static float FromGround(float groundMeters) => groundMeters * GroundToSpaceFactor;

        /// <summary>
        /// Command range: the base plus a ground command-radius bonus (Leadership's meters, scaled like every
        /// other ground radius) plus any extra space meters the caller adds.
        /// </summary>
        public static float GetCommandRange(int groundBonusMeters, float extraMeters = 0f)
        {
            return CommandRange + FromGround(Math.Max(0, groundBonusMeters)) + Math.Max(0f, extraMeters);
        }
    }
}
