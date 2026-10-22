using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>
/// The contract every crafting minigame engine implements. An engine is pure C#: it owns the meters, actions, previews and
/// opportunities, and a window only binds them and forwards input.
/// </summary>
public interface ICraftMinigameEngine
{
    CraftMinigameType Type { get; }

    MinigameStatus Status { get; }

    /// <summary>The shared meters: progress, quality, integrity and CP, each with its maximum.</summary>
    MinigameMeters Meters { get; }

    /// <summary>The number of actions performed so far.</summary>
    int Turn { get; }

    /// <summary>The opportunity announced for an upcoming turn, or null when none is.</summary>
    MinigameOpportunity NextOpportunity { get; }

    IReadOnlyList<MinigameActionInfo> Actions { get; }

    /// <summary>Shows what an action costs and every result it can have, using the same rules <see cref="Perform"/> resolves.</summary>
    MinigameActionPreview Preview(string actionId);

    IReadOnlyList<MinigameActionPreview> PreviewAll();

    /// <summary>Performs an action. A refused action changes nothing and spends no chance.</summary>
    MinigameActionResult Perform(string actionId);
}
