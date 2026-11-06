namespace SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;

/// <summary>
/// A point or a push in flavor space: Savory, Sweet and Spice, each a whole number. A dish's profile is a point in
/// <c>0..KitchenTuning.AxisMax</c> on every axis; an ingredient or a drift is a push added to it.
/// </summary>
public readonly record struct FlavorVector(int Savory, int Sweet, int Spice)
{
    public static readonly FlavorVector Zero = new(0, 0, 0);

    public bool IsZero => Savory == 0 && Sweet == 0 && Spice == 0;

    public int Get(KitchenAxis axis) => axis switch
    {
        KitchenAxis.Savory => Savory,
        KitchenAxis.Sweet => Sweet,
        KitchenAxis.Spice => Spice,
        _ => throw new ArgumentOutOfRangeException(nameof(axis))
    };

    /// <summary>The vector with one axis set to <paramref name="value"/>.</summary>
    public FlavorVector With(KitchenAxis axis, int value) => axis switch
    {
        KitchenAxis.Savory => this with { Savory = value },
        KitchenAxis.Sweet => this with { Sweet = value },
        KitchenAxis.Spice => this with { Spice = value },
        _ => throw new ArgumentOutOfRangeException(nameof(axis))
    };

    public static FlavorVector operator +(FlavorVector left, FlavorVector right) =>
        new(left.Savory + right.Savory, left.Sweet + right.Sweet, left.Spice + right.Spice);

    public static FlavorVector operator -(FlavorVector left, FlavorVector right) =>
        new(left.Savory - right.Savory, left.Sweet - right.Sweet, left.Spice - right.Spice);

    /// <summary>Every axis multiplied by <paramref name="percent"/> / 100 and rounded half away from zero.</summary>
    public FlavorVector Scaled(int percent) =>
        new(ScaleAxis(Savory, percent), ScaleAxis(Sweet, percent), ScaleAxis(Spice, percent));

    /// <summary>Every axis multiplied by <paramref name="numerator"/> / <paramref name="denominator"/> and rounded half away from zero.</summary>
    public FlavorVector ScaledBy(int numerator, int denominator) =>
        new(
            (int)Math.Round(Savory * (double)numerator / denominator, MidpointRounding.AwayFromZero),
            (int)Math.Round(Sweet * (double)numerator / denominator, MidpointRounding.AwayFromZero),
            (int)Math.Round(Spice * (double)numerator / denominator, MidpointRounding.AwayFromZero));

    /// <summary>Every axis kept inside 0 to <paramref name="max"/>.</summary>
    public FlavorVector Clamped(int max) =>
        new(Math.Clamp(Savory, 0, max), Math.Clamp(Sweet, 0, max), Math.Clamp(Spice, 0, max));

    /// <summary>The straight-line distance to another point.</summary>
    public double DistanceTo(FlavorVector other)
    {
        var savory = (double)(Savory - other.Savory);
        var sweet = (double)(Sweet - other.Sweet);
        var spice = (double)(Spice - other.Spice);
        return Math.Sqrt(savory * savory + sweet * sweet + spice * spice);
    }

    /// <summary>The length of the vector.</summary>
    public double Length => DistanceTo(Zero);

    public override string ToString() => $"({Savory}, {Sweet}, {Spice})";

    private static int ScaleAxis(int value, int percent) =>
        (int)Math.Round(value * percent / 100.0, MidpointRounding.AwayFromZero);
}
