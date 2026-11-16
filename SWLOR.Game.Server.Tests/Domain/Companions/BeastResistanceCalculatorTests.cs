using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Companions;

/// <summary>
/// A beast's defense and resistance bonuses are a share of the level's maximum, set by a purity from 0 to 100, and each
/// purity travels on an egg as an incubation property named after its damage or resistance type.
/// </summary>
[TestFixture]
public class BeastResistanceCalculatorTests
{
    private FacadeContainer _container = null!;
    private IDisposable _scope = null!;

    private IResistanceService Resistance => _container.Get<IResistanceService>();
    private ICombatDamageCatalogService Catalog => _container.Get<ICombatDamageCatalogService>();

    [OneTimeSetUp]
    public void BuildContainer() => _container = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeContainer() => _container.Dispose();

    [SetUp]
    public void Install() => _scope = _container.Install();

    [TearDown]
    public void Uninstall() => _scope.Dispose();

    [Test]
    public void RandomPurities_CoverEveryDefenseAndResistanceTypeInTheRequestedRange()
    {
        var defense = BeastResistanceCalculator.CreateRandomDefensePurities(3, 6);
        var resistance = BeastResistanceCalculator.CreateRandomResistancePurities(40, 50);

        defense.Keys.Should().BeEquivalentTo(Catalog.GetDefenseDamageTypes());
        defense.Values.Should().OnlyContain(v => v >= 3 && v < 6);
        resistance.Keys.Should().BeEquivalentTo(Resistance.GetAllResistanceTypes());
        resistance.Values.Should().OnlyContain(v => v >= 40 && v < 50);
    }

    [Test]
    public void RandomPurities_DefaultToZeroThroughNine()
    {
        BeastResistanceCalculator.CreateRandomDefensePurities().Values.Should().OnlyContain(v => v >= 0 && v < 10);
        BeastResistanceCalculator.CreateRandomResistancePurities().Values.Should().OnlyContain(v => v >= 0 && v < 10);
    }

    [Test]
    public void ChosenPurities_FillEveryTypeAndDefaultTheMissingOnesToZero()
    {
        var defenseType = Catalog.GetDefenseDamageTypes()[0];
        var resistanceType = Resistance.GetAllResistanceTypes()[0];

        var defense = BeastResistanceCalculator.CreateDefensePurities(new Dictionary<CombatDamageType, int> { [defenseType] = 60 });
        var resistance = BeastResistanceCalculator.CreateResistancePurities(new Dictionary<ResistanceType, int> { [resistanceType] = 30 });

        defense[defenseType].Should().Be(60);
        defense.Where(pair => pair.Key != defenseType).Should().OnlyContain(pair => pair.Value == 0);
        resistance[resistanceType].Should().Be(30);
        resistance.Where(pair => pair.Key != resistanceType).Should().OnlyContain(pair => pair.Value == 0);
    }

    [Test]
    public void NoChosenPurities_MeansAllZero()
    {
        BeastResistanceCalculator.CreateDefensePurities(null!).Values.Should().OnlyContain(v => v == 0);
        BeastResistanceCalculator.CreateResistancePurities(null!).Values.Should().OnlyContain(v => v == 0);
    }

    [Test]
    public void EveryDefenseTypeHasAnIncubationStatThatMapsBack()
    {
        foreach (var type in Catalog.GetDefenseDamageTypes())
        {
            BeastResistanceCalculator.TryGetDefensePurityIncubationStatType(type, out var stat).Should().BeTrue($"{type}");
            stat.ToString().Should().EndWith("DefensePurity");
            BeastResistanceCalculator.TryGetDefenseType(stat, out var back).Should().BeTrue();
            back.Should().Be(type);
        }

        BeastResistanceCalculator.GetDefensePurityIncubationStatTypes().Should().HaveCount(Catalog.GetDefenseDamageTypes().Count);
    }

    [Test]
    public void EveryResistanceTypeHasAnIncubationStatThatMapsBack()
    {
        foreach (var type in Resistance.GetAllResistanceTypes())
        {
            BeastResistanceCalculator.TryGetResistancePurityIncubationStatType(type, out var stat).Should().BeTrue($"{type}");
            stat.ToString().Should().EndWith("ResistancePurity");
            BeastResistanceCalculator.TryGetResistanceType(stat, out var back).Should().BeTrue();
            back.Should().Be(type);
        }

        BeastResistanceCalculator.GetResistancePurityIncubationStatTypes().Should().HaveCount(Resistance.GetAllResistanceTypes().Count);
    }

    [Test]
    public void AStatThatIsNotAPurityOfThatKindIsNotMapped()
    {
        BeastResistanceCalculator.TryGetDefenseType(IncubationStatType.AttackPurity, out var damage).Should().BeFalse();
        damage.Should().Be(CombatDamageType.Invalid);
        BeastResistanceCalculator.TryGetResistanceType(IncubationStatType.AttackPurity, out var resistance).Should().BeFalse();
        resistance.Should().Be(ResistanceType.Invalid);

        var someDefense = Catalog.GetDefenseDamageTypes()[0];
        BeastResistanceCalculator.TryGetDefensePurityIncubationStatType(someDefense, out var defenseStat).Should().BeTrue();
        BeastResistanceCalculator.TryGetResistanceType(defenseStat, out _).Should().BeFalse("a defense purity is not a resistance purity");
    }

    [Test]
    public void ATypeThatIsNotAValidDefenseOrResistanceHasNoIncubationStat()
    {
        BeastResistanceCalculator.TryGetDefensePurityIncubationStatType(CombatDamageType.Invalid, out var defenseStat).Should().BeFalse();
        defenseStat.Should().Be(IncubationStatType.Invalid);
        BeastResistanceCalculator.TryGetResistancePurityIncubationStatType(ResistanceType.Invalid, out var resistanceStat).Should().BeFalse();
        resistanceStat.Should().Be(IncubationStatType.Invalid);
    }

    [Test]
    public void ABonusIsThePurityShareOfTheLevelMaximumRoundedDown()
    {
        var damageType = Catalog.GetDefenseDamageTypes()[0];
        var resistanceType = Resistance.GetAllResistanceTypes()[0];
        var level = new BeastLevel();
        level.MaxDefenseBonuses[damageType] = 50;
        level.MaxResistanceBonuses[resistanceType] = 30;
        var beast = new Beast();
        beast.DefensePurities[damageType] = 50;
        beast.ResistancePurities[resistanceType] = 50;

        BeastResistanceCalculator.CalculateDefenseBonus(level, beast, damageType).Should().Be(25);
        BeastResistanceCalculator.CalculateResistanceBonus(level, beast, resistanceType).Should().Be(15);

        beast.ResistancePurities[resistanceType] = 33;
        BeastResistanceCalculator.CalculateResistanceBonus(level, beast, resistanceType).Should().Be(9, "9.9 rounds down");
    }

    [Test]
    public void AMissingPurityOrMaximumCountsAsZero()
    {
        var damageType = Catalog.GetDefenseDamageTypes()[0];
        var resistanceType = Resistance.GetAllResistanceTypes()[0];
        var bare = new Beast { DefensePurities = null!, ResistancePurities = null! };
        var emptyLevel = new BeastLevel { MaxDefenseBonuses = null!, MaxResistanceBonuses = null! };

        BeastResistanceCalculator.GetDefensePurity(bare, damageType).Should().Be(0);
        BeastResistanceCalculator.GetResistancePurity(bare, resistanceType).Should().Be(0);
        BeastResistanceCalculator.GetMaxResistanceBonus(emptyLevel, resistanceType).Should().Be(0);
        BeastResistanceCalculator.CalculateDefenseBonus(emptyLevel, bare, damageType).Should().Be(0);
        BeastResistanceCalculator.CalculateResistanceBonus(emptyLevel, bare, resistanceType).Should().Be(0);
    }

    [Test]
    public void APurityIsReadFromABeastAndFromAnIncubationJob()
    {
        var damageType = Catalog.GetDefenseDamageTypes()[0];
        var resistanceType = Resistance.GetAllResistanceTypes()[0];
        var beast = new Beast();
        beast.DefensePurities[damageType] = 12;
        beast.ResistancePurities[resistanceType] = 34;
        var job = new IncubationJob();
        job.DefensePurities[damageType] = 56;

        BeastResistanceCalculator.GetDefensePurity(beast, damageType).Should().Be(12);
        BeastResistanceCalculator.GetResistancePurity(beast, resistanceType).Should().Be(34);
        BeastResistanceCalculator.GetDefensePurity(job, damageType).Should().Be(56);
        BeastResistanceCalculator.GetDefensePurity(new IncubationJob { DefensePurities = null! }, damageType).Should().Be(0);
    }
}
