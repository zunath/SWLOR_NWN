using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Service;

public class StatusEffectStatConsumptionTests
{
    // The code under test reaches the Stat facade, which resolves its instance from a container.
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

    [TestCase(1, 8)]
    [TestCase(2, 14)]
    public void EvasiveChallenge_ConsumesItsRefundWithoutRemovingEvasion(int rank, int evasion)
    {
        IStatusEffect effect = rank == 1
            ? new EvasiveChallenge1SelfStatusEffect()
            : new EvasiveChallenge2SelfStatusEffect();
        var tracker = new CreatureStatusEffect();
        tracker.Add(effect);

        tracker.ConsumeStat(StatType.AvoidedAttackSingleStaminaRestore);
        tracker.ConsumeStat(StatType.AvoidedAttackSingleStaminaRestore);

        tracker.GetAllEffects().Should().ContainSingle().Which.Should().BeSameAs(effect);
        tracker.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(evasion);
        tracker.StatGroup.Stats[StatType.AvoidedAttackSingleStaminaRestore].Should().Be(0);
        effect.IsFlaggedForRemoval.Should().BeFalse();

        tracker.Remove(effect);
        tracker.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(0);
        tracker.StatGroup.Stats[StatType.AvoidedAttackSingleStaminaRestore].Should().Be(0);

        tracker.Add(rank == 1 ? new EvasiveChallenge1SelfStatusEffect() : new EvasiveChallenge2SelfStatusEffect());
        tracker.StatGroup.Stats[StatType.AvoidedAttackSingleStaminaRestore].Should().Be(1,
            "a fresh cast must restore the one-use refund");
    }
}
