namespace SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;

/// <summary>
/// The recipe's target zone in flavor space. A dish scores 100% quality while its profile is inside the inner radius of the
/// center, and the quality falls in a straight line to 0% at the outer radius.
/// </summary>
public sealed record KitchenTarget
{
    public KitchenTarget(FlavorVector center, int innerRadius, int outerRadius)
    {
        if (innerRadius < 0)
            throw new ArgumentOutOfRangeException(nameof(innerRadius), "The inner radius cannot be negative.");
        if (outerRadius <= innerRadius)
            throw new ArgumentOutOfRangeException(nameof(outerRadius), "The outer radius must be larger than the inner radius.");

        Center = center;
        InnerRadius = innerRadius;
        OuterRadius = outerRadius;
    }

    public FlavorVector Center { get; }

    public int InnerRadius { get; }

    public int OuterRadius { get; }

    public double DistanceTo(FlavorVector profile) => profile.DistanceTo(Center);

    /// <summary>The quality percentage of a profile, 0 to 100.</summary>
    public int QualityPercent(FlavorVector profile)
    {
        var distance = DistanceTo(profile);
        if (distance <= InnerRadius)
            return KitchenTuning.QualityMax;
        if (distance >= OuterRadius)
            return 0;

        var fraction = (OuterRadius - distance) / (OuterRadius - InnerRadius);
        return (int)Math.Round(KitchenTuning.QualityMax * fraction, MidpointRounding.AwayFromZero);
    }

    /// <summary>The range the perfect zone covers on one axis, inside 0 to <see cref="KitchenTuning.AxisMax"/>.</summary>
    public (int From, int To) PerfectRange(KitchenAxis axis) => RangeOn(axis, InnerRadius);

    /// <summary>The range inside which quality is above 0 on one axis, inside 0 to <see cref="KitchenTuning.AxisMax"/>.</summary>
    public (int From, int To) ToleranceRange(KitchenAxis axis) => RangeOn(axis, OuterRadius);

    private (int From, int To) RangeOn(KitchenAxis axis, int radius)
    {
        var center = Center.Get(axis);
        return (Math.Max(0, center - radius), Math.Min(KitchenTuning.AxisMax, center + radius));
    }
}
