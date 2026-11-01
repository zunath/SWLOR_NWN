namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>A hidden signature's kind (plan section 24). Signatures are held in memory and never saved.</summary>
    public enum SignatureType
    {
        Invalid = 0,
        Rift = 1,
        CombatSite = 2,
        DataSite = 3,
        CacheSite = 4,
        GasSite = 5,
        Derelict = 6,
    }
}
