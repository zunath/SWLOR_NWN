using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The riders restoring FP or Stamina grants (part 8 of the stat split): each needs a value and a duration, reads them
/// through the stat reader its caller passes, and applies its status effect to the creature that was restored.
/// </summary>
[TestFixture]
public class ResourceRestoredEffectsServiceTests
{
    private const uint Creature = 11;

    private Dictionary<(uint, StatType), int> _stats = null!;
    private IStatService _statReader = null!;
    private ScriptedProxy _statCalls = null!;
    private ScriptedProxy _statusCalls = null!;
    private ResourceRestoredEffectsService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _stats = new();
        _statReader = ScriptedProxy.Create<IStatService>(out _statCalls);
        _statCalls.On(nameof(IStatService.GetStatAdjustment), args => _stats.GetValueOrDefault(((uint)args[0]!, (StatType)args[1]!)));
        var statusEffect = ScriptedProxy.Create<IStatusEffectApplicationService>(out _statusCalls);
        _statusCalls.On(nameof(IStatusEffectApplicationService.ApplyStatusEffect), _ => true);
        _service = new ResourceRestoredEffectsService(statusEffect);
    }

    private void Stat(StatType stat, int value) => _stats[(Creature, stat)] = value;

    [Test]
    public void TheRidersNeedAValueAndADuration()
    {
        _service.ApplyFPRestoredEffects(Creature, _statReader);
        _service.ApplyStaminaRestoredEffects(Creature, _statReader);

        Stat(StatType.RestoredFPForceAttackPercentAdjustment, 5);
        Stat(StatType.RestoredStaminaAttackDurationSeconds, 3);
        _service.ApplyFPRestoredEffects(Creature, _statReader);
        _service.ApplyStaminaRestoredEffects(Creature, _statReader);

        _statusCalls.Calls.Should().BeEmpty("each rider lacks either its value or its duration");
    }

    [Test]
    public void RestoringFPGrantsTheForceAttackRiderToTheCreature()
    {
        Stat(StatType.RestoredFPForceAttackPercentAdjustment, 5);
        Stat(StatType.RestoredFPForceAttackDurationSeconds, 4);

        _service.ApplyFPRestoredEffects(Creature, _statReader);

        var call = _statusCalls.Calls.Should().ContainSingle().Which;
        call.Arguments[0].Should().Be(Creature);
        call.Arguments[1].Should().Be(Creature);
        call.Arguments[2].Should().BeOfType<ForceLensForceAttackStatusEffect>();
        call.Arguments[3].Should().Be(4f);
        _statCalls.Calls.Select(read => read.Arguments[1]).Should().Equal(
            StatType.RestoredFPForceAttackPercentAdjustment, StatType.RestoredFPForceAttackDurationSeconds);
    }

    [Test]
    public void RestoringStaminaGrantsTheAttackRiderToTheCreature()
    {
        Stat(StatType.RestoredStaminaAttackPercentAdjustment, -6);
        Stat(StatType.RestoredStaminaAttackDurationSeconds, 3);

        _service.ApplyStaminaRestoredEffects(Creature, _statReader);

        var call = _statusCalls.Calls.Should().ContainSingle().Which;
        call.Arguments[0].Should().Be(Creature);
        call.Arguments[2].Should().BeOfType<ForceLensAttackStatusEffect>("a negative value is still a value");
        call.Arguments[3].Should().Be(3f);
        _statCalls.Calls.Select(read => read.Arguments[1]).Should().Equal(
            StatType.RestoredStaminaAttackPercentAdjustment, StatType.RestoredStaminaAttackDurationSeconds);
    }

    [Test]
    public void ThePoolsReachTheRidersAsTheirListener()
    {
        IResourceRestoredListener listener = _service;
        Stat(StatType.RestoredFPForceAttackPercentAdjustment, 1);
        Stat(StatType.RestoredFPForceAttackDurationSeconds, 1);

        listener.ApplyFPRestoredEffects(Creature, _statReader);

        _statusCalls.Calls.Should().ContainSingle();
    }
}
