namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// What a space ability's weapon shot did. When <see cref="Fired"/> is false nothing happened and
    /// <see cref="Error"/> says why; the ability should not charge its cost.
    /// </summary>
    public class SpaceFireResult
    {
        public bool Fired { get; set; }
        public string Error { get; set; } = string.Empty;

        /// <summary>The target the shot was fired at.</summary>
        public uint Target { get; set; }

        /// <summary>The hit, critical and damage of the shot. Shield and hull damage fill in when it lands.</summary>
        public ShipShotResult Shot { get; set; }

        /// <summary>The module that fired, and its hotbar slot number (1-30).</summary>
        public ShipModuleStep Module { get; set; }
        public int Slot { get; set; }

        public bool IsHit => Fired && Shot is { IsHit: true };
        public bool IsCritical => Fired && Shot is { IsCritical: true };

        public static SpaceFireResult Failure(string error) => new() { Error = error };
    }
}
