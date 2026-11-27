namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>The numbers a ship's cloak runs with, fixed when the Cloaking Device is switched on.</summary>
    public class ShipCloakProfile
    {
        /// <summary>Seconds from the press until the ship vanishes.</summary>
        public double EngageSeconds { get; init; }

        /// <summary>The share of its speed the ship keeps while cloaking and cloaked (1.0 is no change).</summary>
        public double SpeedMultiplier { get; init; } = 1.0;

        /// <summary>The range in meters within which another ship sees through the cloak.</summary>
        public int RevealMeters { get; init; }
    }
}
