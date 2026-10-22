namespace SWLOR.Game.Server.Service.FishingService
{
    /// <summary>How hard a fish is to find. A rare fish also fights with trickier move patterns.</summary>
    public enum FishRarity
    {
        Common = 0,

        /// <summary>The special fish that appear at one random spot per server start.</summary>
        Rare = 1
    }
}
