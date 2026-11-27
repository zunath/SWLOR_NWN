namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>A ship a Sensor Sweep found.</summary>
    public class SensorSweepContact
    {
        public string Name { get; init; }
        public float Distance { get; init; }
        public ShipSize Size { get; init; }
        public bool Hostile { get; init; }
        public bool Cloaked { get; init; }
    }
}
