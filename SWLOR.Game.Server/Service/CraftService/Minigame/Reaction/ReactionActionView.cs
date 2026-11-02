namespace SWLOR.Game.Server.Service.CraftService.Minigame.Reaction;

/// <summary>An action as the window shows it: a one-line summary beside the button and a longer tooltip.</summary>
public sealed record ReactionActionView(string Summary, string Tooltip, ReactionActionKind Kind);
