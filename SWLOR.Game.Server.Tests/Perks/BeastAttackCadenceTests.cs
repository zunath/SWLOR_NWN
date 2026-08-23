using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

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

            var delays = beast.Levels.Values.Select(level => level.AttackDelay).Distinct().ToList();
            delays.Should().ContainSingle($"{beastType} should use one species cadence at every level");
            delays[0].Should().BeOneOf(
                ItemPropertyAttackDelay.Delay200,
                ItemPropertyAttackDelay.Delay210,
                ItemPropertyAttackDelay.Delay220,
                ItemPropertyAttackDelay.Delay230,
                ItemPropertyAttackDelay.Delay240);
        }

        beasts.Values
            .Select(beast => beast.Levels[1].AttackDelay)
            .Distinct()
            .Should()
            .BeEquivalentTo(new[]
            {
                ItemPropertyAttackDelay.Delay200,
                ItemPropertyAttackDelay.Delay210,
                ItemPropertyAttackDelay.Delay220,
                ItemPropertyAttackDelay.Delay230,
                ItemPropertyAttackDelay.Delay240,
            });
    }

    [TestCase(ItemPropertyAttackDelay.Delay200, 1583)]
    [TestCase(ItemPropertyAttackDelay.Delay210, 1750)]
    [TestCase(ItemPropertyAttackDelay.Delay220, 1916)]
    [TestCase(ItemPropertyAttackDelay.Delay230, 2083)]
    [TestCase(ItemPropertyAttackDelay.Delay240, 2250)]
    public void BeastDelayBands_ProduceTheExpectedUnhastedCadence(
        ItemPropertyAttackDelay delay,
        int expectedMilliseconds)
    {
        var calculatedDelay = Combat.CalculateAttackDelayMilliseconds((int)delay * 10, 0, 0, 0);

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
            "ItemPropertyCustom(ItemPropertyType.Delay, -1, (int)level.AttackDelay)",
            "the generated species cadence must replace the generic beast-claw delay at runtime");
    }

    [Test]
    public void BeastGenerator_RequiresAttackDelayFromTheDesignBible()
    {
        var root = FindRepositoryRoot();
        var generator = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.CLI", "BeastCodeBuilder.cs"));
        var template = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.CLI", "Templates", "beast_level_template.txt"));

        generator.Should().Contain("row[\"Attack Delay\"]");
        generator.Should().Contain("FormatAttackDelay(row[\"Attack Delay\"])");
        template.Should().Contain(".AttackDelay(%%ATTACKDELAY%%)");
    }

    [Test]
    public void AttackDelayEnum_MatchesEveryUsableIprpDelayRow()
    {
        var root = FindRepositoryRoot();
        var rows = File.ReadLines(Path.Combine(root.FullName, "SWLOR_Haks", "sw_2da", "iprp_delay.2da"))
            .Select(line => line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            .Where(columns =>
                columns.Length >= 3 &&
                int.TryParse(columns[0], out _) &&
                columns[2] != "****")
            .Select(columns => (Row: int.Parse(columns[0]), Label: columns[2]))
            .ToList();

        var enumValues = Enum.GetValues<ItemPropertyAttackDelay>()
            .Where(delay => delay != ItemPropertyAttackDelay.Invalid)
            .ToList();
        enumValues.Should().HaveSameCount(rows);

        foreach (var (row, label) in rows)
        {
            Enum.TryParse<ItemPropertyAttackDelay>($"Delay{label}", out var delay).Should().BeTrue();
            ((int)delay).Should().Be(row, $"Delay{label} must point to iprp_delay.2da row {row}");
        }
    }

    [TestCase(0)]
    [TestCase(10)]
    [TestCase(101)]
    public void BeastBuilder_RejectsPlaceholderOrUndefinedAttackDelayRows(int costTableValue)
    {
        var builder = new BeastBuilder()
            .Create(BeastType.Gizka)
            .AddLevel();

        var assign = () => builder.AttackDelay((ItemPropertyAttackDelay)costTableValue);

        assign.Should().Throw<ArgumentOutOfRangeException>();
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
