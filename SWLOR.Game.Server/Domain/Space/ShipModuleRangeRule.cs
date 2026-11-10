namespace SWLOR.Game.Server.Domain.Space
{
    /// <summary>
    /// Determines which selected targets a ship module's maximum distance applies to.
    /// </summary>
    public enum ShipModuleRangeRule
    {
        /// <summary>The module acts on the selected target, so that target must always be in range.</summary>
        AnyTarget = 0,

        /// <summary>The module falls back to the user's own ship on an enemy target, so only other targets must be in range.</summary>
        NonEnemyTarget = 1,

        /// <summary>The module is centered on the user's ship and never reads the selected target.</summary>
        NoTarget = 2,
    }
}
