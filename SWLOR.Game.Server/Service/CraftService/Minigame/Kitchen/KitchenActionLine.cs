namespace SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;

/// <summary>An action's row in the Kitchen window: its one-line summary, how it is drawn, and the full text for its tooltip.</summary>
public sealed record KitchenActionLine(string Summary, KitchenActionTone Tone, string Tooltip);
