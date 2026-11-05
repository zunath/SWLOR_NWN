namespace SWLOR.Game.Server.Service.CraftService.Minigame;

public static class MinigameStateExtensions
{
    /// <summary>The state with its shared meters moved by <paramref name="delta"/> and kept inside their ranges.</summary>
    public static TState Adjust<TState>(this TState state, MinigameMeterDelta delta)
        where TState : IMinigameState<TState> =>
        state.WithMeters(state.Meters.Apply(delta));
}
