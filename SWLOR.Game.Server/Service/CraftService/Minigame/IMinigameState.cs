namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>
/// Everything an engine knows about an attempt, as one immutable value. The shared meters and the telegraphed opportunity live
/// here, and so does every engine-specific value (Thermal, Load, flavor, evidence, Tension). An action changes the attempt only by
/// returning a new state, which is what lets a preview show every change by running the same code.
/// </summary>
public interface IMinigameState
{
    MinigameMeters Meters { get; }

    /// <summary>The opportunity announced for the next turn, or null when none is.</summary>
    MinigameOpportunity NextOpportunity { get; }
}

/// <summary>A minigame state that can be copied with different shared meters; implement it on a record.</summary>
public interface IMinigameState<TState> : IMinigameState
    where TState : IMinigameState<TState>
{
    TState WithMeters(MinigameMeters meters);
}
