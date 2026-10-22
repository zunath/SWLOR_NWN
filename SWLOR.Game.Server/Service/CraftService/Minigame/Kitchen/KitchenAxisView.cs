namespace SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;

/// <summary>One flavor axis as a window shows it: where the dish stands, the target's center, and both zones along the axis.</summary>
/// <param name="Axis">The axis.</param>
/// <param name="Name">Savory, Sweet or Spice.</param>
/// <param name="Value">The dish's flavor on the axis, 0 to <see cref="KitchenTuning.AxisMax"/>.</param>
/// <param name="Center">The target's center on the axis.</param>
/// <param name="PerfectFrom">The low end of the range the inner radius covers on the axis.</param>
/// <param name="PerfectTo">The high end of the range the inner radius covers on the axis.</param>
/// <param name="ToleranceFrom">The low end of the range the outer radius covers on the axis.</param>
/// <param name="ToleranceTo">The high end of the range the outer radius covers on the axis.</param>
public sealed record KitchenAxisView(
    KitchenAxis Axis,
    string Name,
    int Value,
    int Center,
    int PerfectFrom,
    int PerfectTo,
    int ToleranceFrom,
    int ToleranceTo);
