using SWLOR.Game.Server.Domain.World;

namespace SWLOR.CraftSim.Legacy.Gathering
{
    /// <summary>One fish the game can hook, with its level.</summary>
    public sealed record FishInfo(FishType Type, string Name, int Level);
}
