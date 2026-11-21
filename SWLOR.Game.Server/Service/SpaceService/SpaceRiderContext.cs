namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// What a rider handler is told when a pending rider is consumed by a landed weapon shot.
    /// </summary>
    public class SpaceRiderContext
    {
        public uint Attacker { get; set; }
        public uint Target { get; set; }
        public SpaceWeaponShot Shot { get; set; }
        public ShipShotResult Result { get; set; }
        public SpaceShotRider Rider { get; set; }
    }
}
