namespace SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;

/// <summary>An action as the window shows it: a one-line summary beside the button and a longer tooltip.</summary>
public sealed record ExperimentActionView(string Summary, string Tooltip, ExperimentActionKind Kind);
