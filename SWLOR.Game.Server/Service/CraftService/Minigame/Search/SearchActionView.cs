namespace SWLOR.Game.Server.Service.CraftService.Minigame.Search;

/// <summary>An action as the window shows it before the click.</summary>
/// <param name="Summary">The exact result: the chances, what it costs, and any ending it causes.</param>
/// <param name="Tooltip">What the action does.</param>
/// <param name="Kind">Which colour the row takes.</param>
public sealed record SearchActionView(string Summary, string Tooltip, SearchActionKind Kind);
