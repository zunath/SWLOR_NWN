using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Social;

/// <summary>Applying, refreshing and reading an Inspired effect, over a recording stand-in for the status effect service.</summary>
[TestFixture]
public class PerformanceInspiredServiceTests
{
    private const uint Creature = 1001;

    private TestInspired _inspired = null!;

    [SetUp]
    public void SetUp() => _inspired = new TestInspired();

    [Test]
    public void ACreatureWithoutTheEffectGetsANewOneWithAtLeastOneSecond()
    {
        var data = new InspiredEffectData { XPBonusPercent = 10 };

        _inspired.Apply(Creature, data, 0.25, false).Should().BeTrue();

        _inspired.Calls.Should().Equal("Apply:1");
        _inspired.Held!.Data.Should().Be(data);
    }

    [Test]
    public void TheSameCompositionOnlyRefreshesTheTime()
    {
        var data = new InspiredEffectData { XPBonusPercent = 10, Requiem = true };
        _inspired.Held = new InspiredStatusEffect(data);
        _inspired.Held.TryConsumeDeathDebtWaiver().Should().BeTrue();

        _inspired.Apply(Creature, data, 90, false).Should().BeFalse();

        _inspired.Calls.Should().Equal("Refresh:90");
        _inspired.Held.HasRequiemAvailable.Should().BeFalse("nothing was banked again");
    }

    [Test]
    public void BankingAgainGivesAFreshRequiemWhenOnlyTheTimeIsRefreshed()
    {
        var data = new InspiredEffectData { Requiem = true };
        _inspired.Held = new InspiredStatusEffect(data);
        _inspired.Held.TryConsumeDeathDebtWaiver();

        _inspired.Apply(Creature, data, 90, true).Should().BeFalse();

        _inspired.Held.HasRequiemAvailable.Should().BeTrue();
    }

    [Test]
    public void ADifferentCompositionReplacesTheEffectAndKeepsASpentRequiem()
    {
        var old = new InspiredStatusEffect(new InspiredEffectData { Requiem = true });
        old.TryConsumeDeathDebtWaiver();
        _inspired.Held = old;
        var data = new InspiredEffectData { Requiem = true, Might = 2 };

        _inspired.Apply(Creature, data, 60, false).Should().BeTrue();
        _inspired.Held!.Data.Should().Be(data);
        _inspired.Held.HasRequiemAvailable.Should().BeFalse("the replaced effect had spent its Requiem");

        _inspired.Held = old;
        _inspired.Apply(Creature, data, 60, true).Should().BeTrue();
        _inspired.Held!.HasRequiemAvailable.Should().BeTrue("banking again resets it");
    }

    [Test]
    public void TheRemainingSecondsAreZeroWithoutTheEffectAndTheEffectsOwnTimeWithIt()
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        _inspired.GetRemainingSeconds(Creature, now).Should().Be(0.0);

        var effect = new InspiredStatusEffect(new InspiredEffectData());
        _inspired.Held = effect;

        _inspired.GetRemainingSeconds(Creature, now).Should().Be(effect.GetRemainingDurationSeconds(now));
    }

    [Test]
    public void SetRemainingRefreshesTheTimeAndResetsTheRequiemOnlyWhenBanking()
    {
        var effect = new InspiredStatusEffect(new InspiredEffectData { Requiem = true });
        effect.TryConsumeDeathDebtWaiver();
        _inspired.Held = effect;

        _inspired.SetRemaining(Creature, 0.1);
        effect.HasRequiemAvailable.Should().BeFalse();

        _inspired.SetRemaining(Creature, 120, bankedAgain: true);

        _inspired.Calls.Should().Equal("Refresh:1", "Refresh:120");
        effect.HasRequiemAvailable.Should().BeTrue();
    }

    [Test]
    public void SetRemainingWithNoEffectOnlyRefreshes()
    {
        _inspired.SetRemaining(Creature, 30, bankedAgain: true);

        _inspired.Calls.Should().Equal("Refresh:30");
    }

    private sealed class TestInspired : PerformanceInspiredService
    {
        public TestInspired()
            : base(FacadeForwarder.For<IStatusEffectService>())
        {
        }

        public InspiredStatusEffect? Held { get; set; }
        public List<string> Calls { get; } = new();

        protected override InspiredStatusEffect GetInspiredEffect(uint creature) => Held!;

        protected override void RefreshDuration(uint creature, float seconds) => Calls.Add($"Refresh:{seconds}");

        protected override bool ApplyEffect(uint creature, InspiredStatusEffect effect, float seconds)
        {
            Calls.Add($"Apply:{seconds}");
            Held = effect;
            return true;
        }
    }
}
