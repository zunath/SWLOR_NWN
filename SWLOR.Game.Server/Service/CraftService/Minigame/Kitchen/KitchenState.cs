namespace SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;

/// <summary>
/// Everything the Kitchen knows about a dish in progress, as one immutable value. The quality meter is never set directly: every
/// state derives it from the flavor profile and the target, so quality always matches where the flavor sits.
/// </summary>
/// <param name="Meters">Doneness (progress), quality, Freshness (integrity) and CP.</param>
/// <param name="NextOpportunity">Peak Aroma when it is announced for the next action, otherwise null.</param>
/// <param name="Target">The recipe's target zone.</param>
/// <param name="Profile">The dish's flavor right now.</param>
/// <param name="Heat">The heat under the pot.</param>
/// <param name="CoverTurns">The turns left on the lid; Freshness loss is halved while it is above 0.</param>
/// <param name="NextDrift">The drift announced for the end of the next turn, before the heat scales it.</param>
/// <param name="Stock">The portions left of each committed ingredient line.</param>
public sealed record KitchenState(
    MinigameMeters Meters,
    MinigameOpportunity NextOpportunity,
    KitchenTarget Target,
    FlavorVector Profile,
    KitchenHeat Heat,
    int CoverTurns,
    FlavorVector NextDrift,
    KitchenStock Stock) : IMinigameState<KitchenState>
{
    /// <summary>The state with other shared meters; quality is re-derived from the profile whatever the new meters say.</summary>
    public KitchenState WithMeters(MinigameMeters meters) =>
        this with { Meters = meters with { Quality = QualityMeter(Target, Profile) } };

    /// <summary>The state with a new flavor profile, kept inside the flavor cube, and the quality that goes with it.</summary>
    public KitchenState WithProfile(FlavorVector profile)
    {
        var clamped = profile.Clamped(KitchenTuning.AxisMax);
        return (this with { Profile = clamped }).WithMeters(Meters);
    }

    /// <summary>The state with one portion of an ingredient line used up.</summary>
    public KitchenState UsePortion(int line) => this with { Stock = Stock.Use(line) };

    /// <summary>The drift that would move the flavor at the end of the next turn at the current heat.</summary>
    public FlavorVector EffectiveNextDrift => NextDrift.Scaled(KitchenTuning.DriftPercent(Heat));

    public static MinigameMeter QualityMeter(KitchenTarget target, FlavorVector profile) =>
        new(target.QualityPercent(profile), KitchenTuning.QualityMax);
}
