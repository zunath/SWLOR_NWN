namespace SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;

/// <summary>The row for one action: the result it promises, a tooltip and how it is coloured.</summary>
public sealed record ProspectingActionView(string Summary, string Tooltip, ProspectingActionKind Kind);
