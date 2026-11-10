namespace SWLOR.Game.Server.Service.CraftService.Minigame.Line;

/// <summary>
/// Everything the Line knows about a fight, as one immutable value. The shared meters hold the fight itself: Progress is the
/// Distance the fish has been reeled in (its maximum less the Distance), Quality is how worn down the fish is (its Stamina
/// maximum less its Stamina), and Integrity is the line's strength (the Tension cap less the Tension). CP is not used.
/// </summary>
/// <param name="Meters">Reeled in, worn down, line strength.</param>
/// <param name="NextOpportunity">Always null: the fish's announced move is the only thing the Line telegraphs.</param>
/// <param name="Move">The move the fish makes this turn, announced before the angler acts.</param>
/// <param name="ThenMove">The move after it, or null when the angler cannot see that far.</param>
/// <param name="Round">The number of turns taken so far; Set Hook is allowed in round 0 only.</param>
public sealed record LineState(
    MinigameMeters Meters,
    MinigameOpportunity NextOpportunity,
    FishMove Move,
    FishMove? ThenMove,
    int Round) : IMinigameState<LineState>
{
    public LineState WithMeters(MinigameMeters meters) => this with { Meters = meters };

    /// <summary>How far the fish is: 0 is landed, the maximum is escaped.</summary>
    public int Distance => Meters.Progress.Max - Meters.Progress.Value;

    /// <summary>The strain on the line: the cap snaps it.</summary>
    public int Tension => Meters.Integrity.Max - Meters.Integrity.Value;

    public int TensionCap => Meters.Integrity.Max;

    /// <summary>The fish's remaining Stamina.</summary>
    public int Stamina => Meters.Quality.Max - Meters.Quality.Value;

    public int StaminaMax => Meters.Quality.Max;

    /// <summary>True once the fish has no Stamina left; an exhausted fish only rests.</summary>
    public bool Exhausted => Stamina <= 0;

    /// <summary>How the fight stands in these meters.</summary>
    public LineEnding Ending =>
        Meters.Progress.IsFull ? LineEnding.Landed
        : Meters.Integrity.IsEmpty ? LineEnding.Snapped
        : Meters.Progress.IsEmpty ? LineEnding.Escaped
        : LineEnding.InProgress;
}
