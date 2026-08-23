using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.BeastMasteryService;

namespace SWLOR.Game.Server.Tests.Perks;

public class BeastAttackCadenceTests
{
    [Test]
    public void EveryBeast_HasOneExplicitConstantAttackDelayAcrossAllLevels()
    {
        var beasts = BuildAllBeasts();

        beasts.Should().NotBeEmpty();
        beasts.Keys.Should().BeEquivalentTo(
            Enum.GetValues<BeastType>().Where(type => type != BeastType.Invalid),
            "every registered beast must declare its own cadence");
        foreach (var (beastType, beast) in beasts)
        {
            beast.Levels.Should().HaveCount(50, $"{beastType} should retain the complete level progression");
            beast.Levels.Keys.Should().BeEquivalentTo(Enumerable.Range(1, 50));

            var delays = beast.Levels.Values.Select(level => level.Delay).Distinct().ToList();
            delays.Should().ContainSingle($"{beastType} should use one species cadence at every level");
            delays[0].Should().BeInRange(20, 24, $"{beastType} should remain inside the balanced natural-weapon cadence bands");
        }

        beasts.Values
            .Select(beast => beast.Levels[1].Delay)
            .Distinct()
            .Should()
            .BeEquivalentTo(new[] { 20, 21, 22, 23, 24 });
    }

    [TestCase(20, 1583)]
    [TestCase(21, 1750)]
    [TestCase(22, 1916)]
    [TestCase(23, 2083)]
    [TestCase(24, 2250)]
    public void BeastDelayBands_ProduceTheExpectedUnhastedCadence(int delay, int expectedMilliseconds)
    {
        var calculatedDelay = Combat.CalculateAttackDelayMilliseconds(delay * 10, 0, 0, 0);

        Combat.CalculateEffectiveAttackDelay(calculatedDelay).Should().Be(expectedMilliseconds);
    }

    [Test]
    public void BeastRuntime_AppliesTheConfiguredDelayToTheNaturalWeapon()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "BeastMastery.cs"));

        source.Should().Contain(
            "ItemPropertyCustom(ItemPropertyType.Delay, -1, level.Delay)",
            "the generated species cadence must replace the generic beast-claw delay at runtime");
    }

    [Test]
    public void BeastGenerator_RequiresAttackDelayFromTheDesignBible()
    {
        var root = FindRepositoryRoot();
        var generator = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.CLI", "BeastCodeBuilder.cs"));
        var template = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.CLI", "Templates", "beast_level_template.txt"));

        generator.Should().Contain("row[\"Attack Delay\"]");
        template.Should().Contain(".Delay(%%DELAY%%)");
    }

    private static Dictionary<BeastType, BeastDetail> BuildAllBeasts()
    {
        var beasts = new Dictionary<BeastType, BeastDetail>();
        var definitionTypes = typeof(IBeastListDefinition).Assembly
            .GetTypes()
            .Where(type =>
                !type.IsAbstract &&
                !type.IsInterface &&
                typeof(IBeastListDefinition).IsAssignableFrom(type));

        foreach (var definitionType in definitionTypes)
        {
            var definition = (IBeastListDefinition)Activator.CreateInstance(definitionType)!;
            foreach (var (beastType, detail) in definition.Build())
            {
                beasts.Add(beastType, detail);
            }
        }

        return beasts;
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
