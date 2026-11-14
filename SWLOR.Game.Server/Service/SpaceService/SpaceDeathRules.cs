namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Pure rules for a destroyed player ship (plan section 9, Death).
    /// </summary>
    public static class SpaceDeathRules
    {
        /// <summary>The chance, in percent, that each installed module drops on a death that drops modules.</summary>
        public const int ModuleDropChancePercent = 65;

        /// <summary>
        /// Seconds after a hostile player's shot lands during which the ship's death counts as player-caused,
        /// whoever lands the final blow: the PvP combat window (<see cref="PvpCombatService.PvpCombatRules.CombatWindowSeconds"/>).
        /// </summary>
        public const int PvpStakesSeconds = 30;

        /// <summary>
        /// Whether a death is player-caused: a player landed the final blow, or any hostile player's shot hit the ship
        /// within <see cref="PvpStakesSeconds"/>. An NPC or a hazard finishing a ship players were fighting does not
        /// spare its modules, and a player finishing a ship NPCs wore down does not take them alone.
        /// </summary>
        public static bool HasPvpStakes(bool killerIsPlayer, double secondsSincePvpHit) =>
            killerIsPlayer || secondsSincePvpHit <= PvpStakesSeconds;

        /// <summary>
        /// Modules drop only when a player ship dies with PvP stakes (<see cref="HasPvpStakes"/>) in Wild Space.
        /// Everywhere else, including every existing orbit, they stay installed.
        /// </summary>
        public static bool ModulesDrop(bool inWildSpace, bool pvpStakes)
        {
            return inWildSpace && pvpStakes;
        }

        /// <summary>
        /// Whether logging out loses the ship as a player-caused death would: in Wild Space within
        /// <see cref="PvpStakesSeconds"/> of a hostile player's hit, so leaving the game never escapes a fight.
        /// </summary>
        public static bool LogoutForfeitsShip(bool inWildSpace, double secondsSincePvpHit) =>
            inWildSpace && secondsSincePvpHit <= PvpStakesSeconds;

        /// <summary>Whether a module drops, given a d100 roll (1-100).</summary>
        public static bool ModuleDrops(int d100Roll)
        {
            return d100Roll <= ModuleDropChancePercent;
        }
    }
}
