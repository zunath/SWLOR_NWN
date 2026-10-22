namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>
/// Implemented by a minigame's recipe parameters when its recipes pay more or less XP per craft than the plain formula, so the
/// minigame keeps at least today's XP per hour (the XP-rate gate). The percentage scales the recipe's base XP, before the first-craft,
/// blueprint and quality bonuses and before a failed craft takes its 15%.
/// </summary>
public interface ICraftXpScale
{
    /// <summary>Base XP is multiplied by this percentage; 100 changes nothing.</summary>
    int XpPercent { get; }
}

public static class CraftXpScale
{
    /// <summary>The base XP of a craft after the recipe's own scale, whatever minigame its parameters belong to.</summary>
    public static int Apply(RecipeMinigameParameters parameters, int baseXp) =>
        parameters is ICraftXpScale scale
            ? (int)Math.Round(baseXp * scale.XpPercent / 100.0, MidpointRounding.AwayFromZero)
            : baseXp;
}
