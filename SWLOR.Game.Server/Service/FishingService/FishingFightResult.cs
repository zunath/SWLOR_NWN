using SWLOR.Game.Server.Service.CraftService.Minigame.Line;

namespace SWLOR.Game.Server.Service.FishingService
{
    /// <summary>How a fight ended and the words the angler is told.</summary>
    /// <param name="Ending">Landed, escaped or snapped.</param>
    /// <param name="Message">The outcome without colour tokens, for the window.</param>
    public sealed record FishingFightResult(LineEnding Ending, string Message);
}
