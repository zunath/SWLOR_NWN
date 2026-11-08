using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.TwinPistol;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Tests.Support;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.Service;

public class FocusedBurstChannelTests
{
    private FacadeContainer _facades = null!;
    private IDisposable _facadeScope = null!;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void InstallFacades() => _facadeScope = _facades.Install();

    [TearDown]
    public void RemoveFacades() => _facadeScope.Dispose();

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(8)]
    [TestCase(17)]
    [TestCase(100)]
    [TestCase(int.MaxValue)]
    public void ThreeVolleys_ConserveTheFullRolledMagnitudeWithoutAMinimumPerPortion(int damage)
    {
        var portions = Enumerable.Range(0, 3)
            .Select(index => WeaponImpactDamagePartition.Partition(damage, index, 3)).ToArray();
        portions.Sum(part => (long)part).Should().Be(damage);
        portions.Should().BeInAscendingOrder();
        portions.Should().OnlyContain(part => part >= 0);
    }

    [Test]
    public void SingleVolley_PreservesAllDamage()
    {
        WeaponImpactDamagePartition.Partition(37, 0, 1).Should().Be(37);
    }

    [Test]
    public void LaterVolleys_RewardCompletingTheChannel_WithoutIncreasingItsBudget()
    {
        Enumerable.Range(0, 3).Select(index => WeaponImpactDamagePartition.Partition(60, index, 3))
            .Should().Equal(10, 20, 30);
        Enumerable.Range(0, 3).Select(index => WeaponImpactDamagePartition.Partition(1, index, 3))
            .Should().Equal(0, 0, 1);
    }

    [TestCase(0)]
    [TestCase(60)]
    [TestCase(-20)]
    public void HostileAbilityThreat_IsOneBudgetAcrossTheChannel(int bonus)
    {
        Enumerable.Range(0, 3).Sum(index => Ability.CalculateHostileAbilityEnmity(bonus, index, 3))
            .Should().Be(Ability.CalculateHostileAbilityEnmity(bonus));
    }

    [TestCase(-1, 3)]
    [TestCase(3, 3)]
    [TestCase(0, 0)]
    public void InvalidPartition_IsRejected(int index, int count)
    {
        var apply = () => WeaponImpactDamagePartition.Partition(37, index, count);
        apply.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void FocusedBurst_IsAFixedChannel_WhilePairedVolleyRemainsImmediate()
    {
        var focus = new FocusedBurstAbilityDefinition().BuildAbilities();
        foreach (var ability in focus.Values)
        {
            ability.IsChanneled.Should().BeTrue();
            ability.HasFixedActivationDelay.Should().BeTrue();
            ability.SuppressesAutoAttacks.Should().BeTrue();
            ability.ActivationDelay(0, 0, ability.AbilityLevel).Should().Be(1.5f);
            ability.ChannelInterruptAction.Should().NotBeNull();
            ability.AuthoredAnimation.Should().Be(AuthoredAnimation.FocusedBurst);
            ability.UsesAuthoredImpactAnimation.Should().BeFalse();
            ability.RecastGroup.Should().Be(RecastGroup.DoubleShot);
            ability.RecastDelay(0).Should().Be(24f);
        }
        var volley = new PairedVolleyAbilityDefinition().BuildAbilities();
        volley.Values.Should().OnlyContain(ability => !ability.IsChanneled && !ability.HasFixedActivationDelay);
        volley.Values.Select(ability => ability.ActivationDelay(0, 0, ability.AbilityLevel))
            .Should().OnlyContain(delay => delay == 0f);
    }

    [Test]
    public void DestroyedOwner_ReleasesTheChannelsDeferredStaminaCostContext()
    {
        // A droid dismissed mid-channel never reaches FinishChannel, so ForgetDestroyed must release
        // the deferred stamina-cost context that BeginChannel took.
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot().FullName,
            "SWLOR.Game.Server", "Domain", "Combat", "TwinPistolAbilityService.cs")).Replace("\r\n", "\n");
        var start = source.IndexOf("public void ForgetDestroyed(", StringComparison.Ordinal);
        var body = source.Substring(start, source.IndexOf("public void InterruptChannel(", start, StringComparison.Ordinal) - start);

        body.Should().Contain("if (_channels.Remove(actor, out var channel))");
        body.Should().Contain("CompleteDeferredAbilityStaminaCostContext(actor, channel.Ability);");
        body.Should().Contain("_activations.Remove(actor);");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return directory ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
