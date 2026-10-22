namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>
/// One result an action can have: its chance, what it does to the shared meters, and any engine state it changes.
/// The same rule produces the preview and the resolution.
/// </summary>
public sealed class MinigameOutcomeRule
{
    public MinigameOutcomeRule(string label, int chancePercent, MinigameMeterDelta effect, Action onApplied = null)
    {
        if (chancePercent < 1 || chancePercent > 100)
            throw new ArgumentOutOfRangeException(nameof(chancePercent), "An outcome's chance is 1 to 100 percent.");

        Label = label;
        ChancePercent = chancePercent;
        Effect = effect;
        OnApplied = onApplied;
    }

    public string Label { get; }

    public int ChancePercent { get; }

    /// <summary>The change to the meters, not counting the action's CP and integrity cost.</summary>
    public MinigameMeterDelta Effect { get; }

    /// <summary>Runs when this outcome is the one that happens, to update engine state such as buffs and opportunities.</summary>
    public Action OnApplied { get; }
}
