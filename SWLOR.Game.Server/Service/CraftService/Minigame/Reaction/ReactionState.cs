namespace SWLOR.Game.Server.Service.CraftService.Minigame.Reaction;

/// <summary>Everything the Reaction engine knows about an attempt: the shared meters and where the cycle stands.</summary>
/// <param name="Meters">Potency (progress), Subtlety (quality), Stability (integrity) and CP.</param>
/// <param name="NextOpportunity">Always null: the visible cycle is the only thing Reaction announces.</param>
/// <param name="CycleIndex">The position in the cycle; the current phase is the cycle's phase at this position.</param>
/// <param name="HoldCycle">True between Dampen and the end of its turn, so the cycle does not advance that turn.</param>
public sealed record ReactionState(
    MinigameMeters Meters,
    MinigameOpportunity NextOpportunity,
    int CycleIndex,
    bool HoldCycle) : IMinigameState<ReactionState>
{
    public ReactionState WithMeters(MinigameMeters meters) => this with { Meters = meters };
}
