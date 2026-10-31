using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
using SWLOR.Game.Server.Service.FishingService;

namespace SWLOR.Game.Server.Tests.Service.Line;

/// <summary>
/// A middling fight: a 20 Tension line, a 6 Distance reel, a fish hooked 16 out that escapes at 30, with 16 Stamina and a pull of 4.
/// The pinned numbers in the Line tests come from this setup.
/// </summary>
internal static class LineTestData
{
    /// <summary>The rolls that pick each move from a common pattern whose weights total 100 (30, 25, 25, 20).</summary>
    public const int RollsRun = 0;
    public const int RollsDive = 30;
    public const int RollsThrash = 55;
    public const int RollsTire = 80;

    public static LineSetup Setup(
        int cap = 20,
        int reel = 6,
        int start = 16,
        int max = 30,
        int stamina = 16,
        int pull = 4,
        LineAnglerEffects effects = null,
        FishRarity rarity = FishRarity.Common) =>
        new(cap, reel, start, max, stamina, pull, LineMovePattern.For(rarity, 20), effects, 20, rarity, 20, 2);

    /// <summary>The roll that makes a common pattern pick the move.</summary>
    public static int Roll(FishMove move) => move switch
    {
        FishMove.Run => RollsRun,
        FishMove.Dive => RollsDive,
        FishMove.Thrash => RollsThrash,
        _ => RollsTire
    };

    /// <summary>An engine whose first move is fixed and whose later moves come from the scripted rolls, one for each turn performed.</summary>
    public static LineEngine Engine(LineSetup setup, FishMove first, params FishMove[] later) =>
        new(new ScriptedMinigameRandom(later.Select(Roll).ToArray()), setup, first);

    /// <summary>An engine for a setup that sees two moves ahead: the first two moves are fixed.</summary>
    public static LineEngine EngineSeeingTwo(LineSetup setup, FishMove first, FishMove then, params FishMove[] later) =>
        new(new ScriptedMinigameRandom(later.Select(Roll).ToArray()), setup, first, then);

    public static LineAnglerEffects SeesTwoMoves => new(ForecastDepth: 2);
}
