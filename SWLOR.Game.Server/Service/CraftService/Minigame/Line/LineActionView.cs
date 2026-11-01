namespace SWLOR.Game.Server.Service.CraftService.Minigame.Line;

/// <summary>An action as the window shows it before the click.</summary>
/// <param name="Summary">The exact result: how Distance, Tension and Stamina move, and any ending it causes.</param>
/// <param name="Tooltip">What the action does, and the note for the fish's current move.</param>
/// <param name="Kind">Which colour the row takes.</param>
public sealed record LineActionView(string Summary, string Tooltip, LineActionKind Kind);
