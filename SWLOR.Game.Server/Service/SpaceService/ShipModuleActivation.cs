namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// How a fitted module is used (plan section 8). Passive modules have no activation and stay Invalid.
    /// </summary>
    public enum ShipModuleActivation
    {
        Invalid = 0,

        /// <summary>Switched on and off by its hotbar feat; fires itself each time its cycle ends.</summary>
        Toggle = 1,

        /// <summary>Delivers a single burst per press and then recasts.</summary>
        OnePress = 2,
    }
}
