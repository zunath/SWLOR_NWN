namespace SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;

/// <summary>Everything the Circuit knows about an attempt: the shared meters, the telegraphed opportunities, the Load and the once-per-craft perks spent.</summary>
/// <param name="Meters">Assembly, Calibration, Integrity and CP.</param>
/// <param name="NextOpportunity">The Clean Signal announced for the next action, or null.</param>
/// <param name="Load">The Load, 0 to <see cref="CircuitRules.MaxLoad"/>.</param>
/// <param name="LaterOpportunity">The Clean Signal announced for the action after the next, which only Diagnostics announces, or null.</param>
/// <param name="ForecastStarted">True once the attempt has rolled its first forecast.</param>
/// <param name="SurgeUsed">True once the craft's Surge has turned an overload into a Discharge.</param>
/// <param name="FailsafeUsed">True once the craft's Failsafe has kept the piece at 1 Integrity.</param>
public sealed record CircuitState(
    MinigameMeters Meters,
    MinigameOpportunity NextOpportunity,
    int Load,
    MinigameOpportunity LaterOpportunity = null,
    bool ForecastStarted = false,
    bool SurgeUsed = false,
    bool FailsafeUsed = false) : IMinigameState<CircuitState>
{
    public CircuitState WithMeters(MinigameMeters meters) => this with { Meters = meters };

    /// <summary>True when a Clean Signal is announced for the next action.</summary>
    public bool HasCleanSignal => NextOpportunity?.Id == CircuitActionIds.CleanSignal;
}
