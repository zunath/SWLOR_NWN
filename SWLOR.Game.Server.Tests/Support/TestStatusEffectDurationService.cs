using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Part 4 of the status effect split, <c>StatusEffectDurationService</c>, with the stat read and the active ability impact
/// answered by the bench.
/// </summary>
public sealed class TestStatusEffectDurationService : StatusEffectDurationService
{
    private readonly StatusEffectBench _bench;

    public TestStatusEffectDurationService(
        StatusEffectBench bench,
        IStatusEffectService statusEffect,
        IStatusResistance resistance,
        ICombatRecorderService combatRecorder)
        : base(statusEffect, resistance, combatRecorder, FacadeForwarder.LazyFor<IStatService>(), FacadeForwarder.For<IActiveAbilityImpact>())
    {
        _bench = bench;
    }

    protected override int GetCreatureStatAdjustment(uint creature, StatType stat) =>
        _bench.Stats.GetValueOrDefault((creature, stat));

    protected override AbilityImpactSummary GetActiveAbilityImpactSummary(uint activator) => _bench.ActiveImpact!;
}
