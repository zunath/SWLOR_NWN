using FluentAssertions;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Fabricator;

namespace SWLOR.Game.Server.Tests.Service.Fabricator;

/// <summary>
/// A level 25 recipe worked by a rank 25 crafter in middling gear. The pinned numbers in the Fabricator tests come from this
/// setup: 890 units of work split 30/30/40 between the Frame, Shell and Fittings, quality shares of 30/30/40, a wear of 4, 69 CP,
/// a 12% Craftsmanship and a 13% Control bonus, and no rank difference.
/// </summary>
internal static class FabricatorTestData
{
    public const int BaseProgress = 89;

    public static FabricatorParameters Parameters(
        int frameSize = 30,
        int shellSize = 30,
        int frameQuality = 30,
        int shellQuality = 30,
        int wear = 4) =>
        new(frameSize, shellSize, frameQuality, shellQuality, wear);

    public static FabricatorSetup Setup(
        int rank = 25,
        int level = 25,
        int craftsmanship = 12,
        int control = 13,
        int gearCp = 20,
        int penalty = 0,
        FabricatorParameters parameters = null) =>
        new(parameters ?? Parameters(), level, BaseProgress, rank, craftsmanship, control, gearCp, penalty);

    public static FabricatorEngine Engine(FabricatorSetup setup, params int[] rolls) =>
        new(new ScriptedMinigameRandom(rolls), setup);

    /// <summary>A forecast roll that announces nothing: every head has a 12 to 15 percent chance, so 100 is above all of them.</summary>
    public const int NoPrime = 100;

    /// <summary>The forecast roll that names the first head that still has work (the Cutter at the start).</summary>
    public const int PrimeFirst = 1;

    /// <summary>A forecast roll that names the second head that still has work, whatever the chance per head is.</summary>
    public static int PrimeSecond => FabricatorRules.PrimeChancePerHeadPercent + 1;

    /// <summary>A forecast roll that names the third head that still has work.</summary>
    public static int PrimeThird => 2 * FabricatorRules.PrimeChancePerHeadPercent + 1;

    /// <summary>Performs actions in order, each with a forecast roll that announces nothing, and checks that each was performed.</summary>
    public static FabricatorEngine Play(FabricatorSetup setup, params string[] actions)
    {
        var engine = Engine(setup, Enumerable.Repeat(NoPrime, actions.Length + 1).ToArray());
        foreach (var action in actions)
            engine.Perform(action).Performed.Should().BeTrue($"{action} should be available");

        return engine;
    }

    /// <summary>The actions that finish every sub-assembly at the setup's strength: Runs on each, retooling between the heads.</summary>
    public static FabricatorEngine Built(FabricatorSetup setup)
    {
        var engine = Engine(setup, Enumerable.Repeat(NoPrime, 80).ToArray());
        foreach (var part in FabricatorHeads.AllParts)
        {
            if (engine.ActiveHead != part.HeadFor())
                engine.Perform(part.HeadFor().RetoolActionId()).Performed.Should().BeTrue();

            while (engine.Part(part).Progress < setup.SizeOf(part))
                engine.Perform(part.RunActionId()).Performed.Should().BeTrue();
        }

        return engine;
    }

    /// <summary>Raises the quality of every sub-assembly to full with Precision Passes, retooling between the heads.</summary>
    public static void Refine(FabricatorEngine engine, int toQuality = FabricatorRules.MaxQuality)
    {
        foreach (var part in FabricatorHeads.AllParts)
        {
            if (engine.ActiveHead != part.HeadFor())
                engine.Perform(part.HeadFor().RetoolActionId()).Performed.Should().BeTrue();

            while (engine.Part(part).Quality < toQuality)
                engine.Perform(FabricatorActionIds.PrecisionPass).Performed.Should().BeTrue();
        }
    }
}
