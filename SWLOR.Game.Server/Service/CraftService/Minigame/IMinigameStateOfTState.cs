namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>A minigame state that can be copied with different shared meters; implement it on a record.</summary>
public interface IMinigameState<TState> : IMinigameState
    where TState : IMinigameState<TState>
{
    TState WithMeters(MinigameMeters meters);
}
