namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>
/// One result an action can have: its chance and the pure transition it makes to the state. The transition returns a new state
/// and changes nothing else, so the preview and the resolution run the same code.
/// </summary>
public sealed class MinigameOutcomeRule<TState>
    where TState : IMinigameState<TState>
{
    public MinigameOutcomeRule(string label, int chancePercent, Func<TState, TState> transition)
    {
        if (chancePercent < 1 || chancePercent > 100)
            throw new ArgumentOutOfRangeException(nameof(chancePercent), "An outcome's chance is 1 to 100 percent.");

        Label = label;
        ChancePercent = chancePercent;
        Transition = transition ?? throw new ArgumentNullException(nameof(transition));
    }

    public string Label { get; }

    public int ChancePercent { get; }

    /// <summary>
    /// The state after this result, before the action's CP and integrity cost (which the engine applies first) and before the
    /// end of the turn (which it applies after).
    /// </summary>
    public Func<TState, TState> Transition { get; }
}
