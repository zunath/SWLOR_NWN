namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Self-buffs a crew member takes from a ship's station (Ace, Bulwark and Ordnance Stance, Ace in the Hole's Dead
    /// Man's Hand) as pure functions. The buff sits on the ship, so the hull takes its posture (Evasion, Defense, speed,
    /// threat), but its shot modifiers and weapon cycles belong to the station weapons of the crew member who took it:
    /// the pilot's own shots leave them out. A buff the pilot takes is the ship's own and reaches the Helm's shots.
    /// </summary>
    public static class SpaceStanceRules
    {
        /// <summary>Who owns a self-buff on the ship: the crew member who took it, or the ship for the pilot's own.</summary>
        public static uint Owner(uint ship, uint buffSource) =>
            buffSource == 0 || buffSource == OBJECT_INVALID ? ship : buffSource;

        /// <summary>Whether a self-buff on the ship reaches a shot or weapon cycle fired with this stat source.</summary>
        public static bool ReachesShooter(uint ship, uint shotSource, uint buffSource) =>
            shotSource != 0 && shotSource != OBJECT_INVALID && shotSource == Owner(ship, buffSource);

        /// <summary>
        /// What a self-buff on the ship adds to a shot stat (<paramref name="amount"/> is the buff's value for it), on
        /// top of the stat read from the shot's stat source (<paramref name="shotSource"/>), which already holds the
        /// ship's statuses when it is the ship itself.
        /// </summary>
        /// <param name="ship">The firing ship: its pilot's creature.</param>
        /// <param name="shotSource">Whose stats the shot reads: the ship, the crew member at a station, or OBJECT_INVALID for a droid.</param>
        /// <param name="buffSource">Who took the buff (the status's source).</param>
        public static int ShotAdjustment(uint ship, uint shotSource, uint buffSource, int amount)
        {
            // The pilot's own buff is already part of the ship's stats, and reaches only the ship's own shots.
            if (Owner(ship, buffSource) == ship)
                return 0;

            // A crew member's buff: the pilot's shots leave it out, and that crew member's shots add it.
            if (shotSource == ship)
                return -amount;
            return ReachesShooter(ship, shotSource, buffSource) ? amount : 0;
        }
    }
}
