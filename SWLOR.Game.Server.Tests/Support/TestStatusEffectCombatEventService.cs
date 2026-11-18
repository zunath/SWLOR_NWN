using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Part 6 of the status effect split, <c>StatusEffectCombatEventService</c>, with the limited attack suppression read answered
/// by the bench.
/// </summary>
public sealed class TestStatusEffectCombatEventService : StatusEffectCombatEventService
{
    private readonly StatusEffectBench _bench;

    public TestStatusEffectCombatEventService(StatusEffectBench bench, IStatusEffectService statusEffect)
        : base(statusEffect, FacadeForwarder.LazyFor<IStatService>())
    {
        _bench = bench;
    }

    protected override bool IsAttackDelayReductionSuppressed(uint creature) => _bench.AttackDelaySuppressed.Contains(creature);
}
