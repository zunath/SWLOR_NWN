namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>How much the Scanner tab shows of a signature at its party's strength (plan section 24).</summary>
    public enum SignatureInfoLevel
    {
        Unknown = 0,
        Type = 1,
        Tier = 2,
        Threat = 3,
        Located = 4,
    }
}
