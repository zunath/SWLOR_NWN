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
        /// Modules drop only when a player ship dies to another player in Wild Space. Everywhere else, including
        /// every existing orbit, they stay installed.
        /// </summary>
        public static bool ModulesDrop(bool inWildSpace, bool killerIsPlayer)
        {
            return inWildSpace && killerIsPlayer;
        }

        /// <summary>Whether a module drops, given a d100 roll (1-100).</summary>
        public static bool ModuleDrops(int d100Roll)
        {
            return d100Roll <= ModuleDropChancePercent;
        }
    }
}
