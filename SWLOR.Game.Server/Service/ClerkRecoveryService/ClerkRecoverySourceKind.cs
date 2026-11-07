namespace SWLOR.Game.Server.Service.ClerkRecoveryService
{
    /// <summary>What a clerk offer is about.</summary>
    public enum ClerkRecoverySourceKind
    {
        Invalid = 0,

        /// <summary>A crop growing in a garden plot.</summary>
        Crop = 1,

        /// <summary>An animal standing in a corral.</summary>
        Animal = 2
    }
}
