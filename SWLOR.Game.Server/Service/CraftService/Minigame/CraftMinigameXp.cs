using SWLOR.Game.Server.Service.CraftService.Minigame.Fabricator;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>
/// The per-craft XP multiplier of each minigame. A minigame whose loop takes longer than the old one, and so would pay less XP per
/// hour, names one multiplier here (the CraftSim XP-rate gate measures what it needs), and every craft of that minigame, failed
/// crafts included, scales its base XP by it before the first-craft, blueprint and quality bonuses. A minigame with no entry
/// pays the base XP of today.
/// </summary>
public static class CraftMinigameXp
{
    /// <summary>The multiplier of a minigame, or 1 when it has none.</summary>
    public static double MultiplierFor(CraftMinigameType? minigame) => minigame switch
    {
        CraftMinigameType.Fabricator => FabricatorRules.XpMultiplier,
        _ => 1.0
    };

    /// <summary>The multiplier of the minigame that crafts a skill's recipes, or 1 for a skill still on the original window.</summary>
    public static double MultiplierFor(SkillType skill) => MultiplierFor(CraftMinigameRouting.GetRoute(skill).Minigame);

    /// <summary>The base XP scaled by a multiplier, rounded to a whole number.</summary>
    public static int Scale(int baseXp, double multiplier) =>
        (int)Math.Round(baseXp * multiplier, MidpointRounding.AwayFromZero);
}
