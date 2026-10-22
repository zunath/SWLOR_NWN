using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;

namespace SWLOR.Game.Server.Tests.Service.Circuit;

/// <summary>
/// A level 25 recipe worked by a rank 25 crafter in middling gear. The pinned numbers in the Circuit tests come from this setup:
/// Assembly 89, Calibration 1296, Integrity 70, 69 CP, a 12% Craftsmanship and a 13% Control bonus, and no rank difference.
/// </summary>
internal static class CircuitTestData
{
    public static CircuitSetup Setup(
        int rank = 25,
        int line = 6,
        int boost = 10,
        int share = 50,
        int level = 25,
        int craftsmanship = 12,
        int control = 13,
        int gearCp = 20,
        int penalty = 0,
        int baseProgress = 89,
        int baseQuality = 1296,
        int integrity = 70) =>
        new(new CircuitParameters(line, boost, share), level, baseProgress, baseQuality, integrity, rank, craftsmanship, control, gearCp, penalty);

    public static CircuitEngine Engine(CircuitSetup setup, int startingLoad = 0, params int[] rolls) =>
        new(new ScriptedMinigameRandom(rolls), setup, startingLoad);

    /// <summary>The outcome roll that always holds: the first outcome takes every roll up to its chance.</summary>
    public const int Holds = 1;

    /// <summary>The outcome roll that always overloads: the overload is the last outcome and takes the highest rolls.</summary>
    public const int Overloads = 100;

    /// <summary>A forecast roll that announces nothing.</summary>
    public const int NoSignal = 100;

    /// <summary>A forecast roll that announces a Clean Signal.</summary>
    public const int Signal = 1;

    /// <summary>
    /// The rolls one performed action uses: the outcome roll when the action has several outcomes, then the forecast roll when
    /// the attempt goes on and Tune is unlocked.
    /// </summary>
    public static int[] Rolls(int? outcomeRoll = null, int forecastRoll = NoSignal) =>
        outcomeRoll.HasValue ? new[] { outcomeRoll.Value, forecastRoll } : new[] { forecastRoll };
}
