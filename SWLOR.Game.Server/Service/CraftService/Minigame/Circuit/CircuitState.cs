namespace SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;

/// <summary>Everything the Circuit knows about an attempt: the shared meters, the telegraphed opportunity and the Load.</summary>
public sealed record CircuitState(MinigameMeters Meters, MinigameOpportunity NextOpportunity, int Load) : IMinigameState<CircuitState>
{
    public CircuitState WithMeters(MinigameMeters meters) => this with { Meters = meters };

    /// <summary>True when a Clean Signal is announced for the next action.</summary>
    public bool HasCleanSignal => NextOpportunity?.Id == CircuitActionIds.CleanSignal;
}
