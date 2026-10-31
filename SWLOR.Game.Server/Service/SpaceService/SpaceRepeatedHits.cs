namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The arithmetic of "each consecutive hit against the same target grants +N DMG, up to a cap" for ship
    /// weapons, in the ground's terms (Rifle Sustained Fire): a hit on a new target, or after the stacks
    /// expired, starts a new run at one stack.
    /// </summary>
    public static class SpaceRepeatedHits
    {
        /// <summary>The most stacks a bonus per hit and a maximum bonus allow (at least one).</summary>
        public static int GetMaximumStacks(int bonusPerHit, int maximumBonus)
        {
            return bonusPerHit <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(maximumBonus / (float)bonusPerHit));
        }

        /// <summary>The DMG the stacks add.</summary>
        public static int GetBonus(int stacks, int bonusPerHit, int maximumBonus)
        {
            return Math.Min(maximumBonus, stacks * bonusPerHit);
        }

        /// <summary>The most energy weapons that each keep their stacks' full DMG.</summary>
        public const int FullShareWeapons = 2;

        /// <summary>
        /// A weapon's share of its own stacks' DMG when the ship fires several energy weapons: the full bonus with one or
        /// two, otherwise the bonus x 2 / the number firing, rounded half away from zero. A ship's guns together get at
        /// most two ground weapons' Sustained Fire, however many it carries.
        /// </summary>
        public static int GetShare(int bonus, int energyWeapons)
        {
            return energyWeapons <= FullShareWeapons
                ? bonus
                : (int)Math.Round(bonus * FullShareWeapons / (double)energyWeapons, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// The run after a landed hit on <paramref name="target"/>: one more stack on the same target while the
        /// run is fresh, otherwise a new run.
        /// </summary>
        public static SpaceRepeatedHitState Advance(
            SpaceRepeatedHitState current,
            uint target,
            int bonusPerHit,
            int maximumBonus,
            int durationSeconds,
            DateTime now)
        {
            var continues = current != null &&
                            current.Target == target &&
                            (now - current.LastHit).TotalSeconds <= durationSeconds;
            var stacks = continues ? current.Stacks : 0;

            return new SpaceRepeatedHitState
            {
                Target = target,
                Stacks = Math.Min(stacks + 1, GetMaximumStacks(bonusPerHit, maximumBonus)),
                LastHit = now
            };
        }
    }
}
