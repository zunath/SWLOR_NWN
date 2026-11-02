namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>One meter as a window shows it: the shared meters and the engine's own, in display order.</summary>
/// <param name="Id">A stable id; the shared meters use <see cref="MinigameMeterIds"/>.</param>
/// <param name="Label">The name shown, such as "Shape" or "Thermal".</param>
/// <param name="Value">The current value.</param>
/// <param name="Min">The lowest value.</param>
/// <param name="Max">The highest value.</param>
/// <param name="Mark">A marked range or line, or null.</param>
public sealed record MinigameMeterView(string Id, string Label, int Value, int Min, int Max, MinigameMeterMark Mark = null);
