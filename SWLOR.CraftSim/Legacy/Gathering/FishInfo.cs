using SWLOR.Game.Server.Service.FishingService;

namespace SWLOR.CraftSim.Legacy.Gathering
{
    /// <summary>One fish the game can hook, with its level.</summary>
    public sealed record FishInfo(FishType Type, string Name, int Level);
}
