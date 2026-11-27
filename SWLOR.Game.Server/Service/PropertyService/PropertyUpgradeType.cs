namespace SWLOR.Game.Server.Service.PropertyService
{
    public enum PropertyUpgradeType
    {
        Invalid = 0,
        CityLevel = 1,
        MedicalCenterLevel = 3,
        StarportLevel = 4,
        CantinaLevel = 5,

        /// <summary>A player station's Jump Beacon level (1-3), which caps the hulls it jumps. Missing on planet cities.</summary>
        JumpBeaconLevel = 6
    }
}
