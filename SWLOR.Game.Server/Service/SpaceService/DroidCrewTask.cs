namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>What a droid at a station does with a toggled module.</summary>
    public enum DroidCrewTask
    {
        /// <summary>The droid leaves the module alone (one-press modules, miners, the injector, utility toggles).</summary>
        None = 0,

        /// <summary>A weapon: switched on whenever the pilot has a hostile target in range.</summary>
        Weapon = 1,

        /// <summary>A shield repairer, generator or hardener: run while shield is low.</summary>
        ShieldSupport = 2,

        /// <summary>A hull repairer or repair field: run while hull is low.</summary>
        HullSupport = 3,

        /// <summary>A Probe Launcher: run whenever there is something to scan, by a droid with Survey Protocol.</summary>
        Survey = 4,

        /// <summary>An electronic warfare array: run while an enemy ship is within its range.</summary>
        ElectronicWarfare = 5,
    }
}
