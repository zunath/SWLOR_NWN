using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Tests.Perks;

/// <summary>
/// An area ability with no target cap pays its per-hit rider once per struck enemy, so its
/// resource payout grows with the size of the pull. Twin Blade's Sweeping Advance is the house
/// pattern: pay per target, but stop at a fixed per-cast ceiling.
/// </summary>
public class PerCastResourceCapTests
{
    [TestCase(4, 12, 1, 4)]
    [TestCase(4, 12, 3, 12)]
    [TestCase(4, 12, 10, 12)]
    [TestCase(5, 15, 2, 10)]
    [TestCase(5, 15, 20, 15)]
    public void PerCastBudget_StopsAtTheCeiling(
        int amountPerHit,
        int maximumPerCast,
        int targetsHit,
        int expectedGranted)
    {
        var budget = new PerCastResourceBudget(amountPerHit, maximumPerCast);
        var granted = 0;
        for (var hit = 0; hit < targetsHit; hit++)
            granted += budget.Take();

        granted.Should().Be(expectedGranted);
        budget.Remaining.Should().Be(maximumPerCast - expectedGranted);
    }

    [Test]
    public void EachCast_GetsAFreshBudget()
    {
        var factory = Factory(4, 12);
        var first = Cast(factory);
        var second = Cast(factory);

        first.Should().NotBeSameAs(second,
            "a new activation must not inherit the previous cast's spent budget");
    }

    [Test]
    public void TheThreeUncappedMimicryRestores_DeclareAPerCastCeiling()
    {
        foreach (var (file, expected) in new[]
                 {
                     ("WardenRendTechniqueAbilityDefinition.cs", "RestoreFPPerHit(4, 12)"),
                     ("WillFractureTechniqueAbilityDefinition.cs", "RestoreFPPerHit(4, 12)"),
                     ("FinalEclipseTechniqueAbilityDefinition.cs", "RestoreFPPerHit(5, 15)")
                 })
        {
            var source = ReadSource("Feature", "AbilityDefinition", "Mimicry", file);
            source.Should().Contain(expected);
            source.Should().NotContain("RestoreFPOnHit(",
                $"{file} strikes an uncapped area, so its FP payout must be bounded per cast");
        }
    }

    [Test]
    public void MaelstromArc_RestoresOncePerCastBehindItsDeflectionCondition()
    {
        var source = ReadSource("Feature", "AbilityDefinition", "Saberstaff", "MaelstromArcAbilityDefinition.cs");
        source.Should().NotContain("RestoreFPOnHit",
            "the authored text restores a flat amount, not one payout per target struck by the cone");
        source.Should().Contain("RestoreFPAfterRangedDeflection = 4");
        source.Should().Contain("RestoreFPAfterRangedDeflection = 8");
        source.Should().Contain("RestoreFPAfterRangedDeflectionWindowSeconds = 30");
    }

    private static Type InnateAbilityType =>
        typeof(IPerkListDefinition).Assembly
            .GetType("SWLOR.Game.Server.Feature.AbilityDefinition.NPC.InnateAbility", throwOnError: true)!;

    private static object Factory(int amountPerHit, int maximumPerCast) =>
        InnateAbilityType
            .GetMethod("RestoreFPPerHit", BindingFlags.Public | BindingFlags.Static)!
            .Invoke(null, new object[] { amountPerHit, maximumPerCast })!;

    private static Action<uint, uint> Cast(object factory) =>
        (Action<uint, uint>)((Delegate)factory).DynamicInvoke()!;

    private static string ReadSource(params string[] pathParts)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        if (directory == null)
            throw new DirectoryNotFoundException("Could not locate SWLOR.Game.Server.sln from the test directory.");

        return File.ReadAllText(Path.Combine(
            new[] { directory.FullName, "SWLOR.Game.Server" }.Concat(pathParts).ToArray()));
    }
}
