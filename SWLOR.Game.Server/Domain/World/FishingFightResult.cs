using SWLOR.Game.Server.Domain.Crafting.Minigame.Line;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>How a fight ended and the words the angler is told.</summary>
    /// <param name="Ending">Landed, escaped or snapped.</param>
    /// <param name="Message">The outcome without colour tokens, for the window.</param>
    public sealed record FishingFightResult(LineEnding Ending, string Message);
}
