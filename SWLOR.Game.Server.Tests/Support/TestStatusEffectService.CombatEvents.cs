namespace SWLOR.Game.Server.Tests.Support;

// Part 6, StatusEffectCombatEventService: the limited attack suppression read.
public sealed partial class TestStatusEffectService
{
    protected override bool IsAttackDelayReductionSuppressed(uint creature) => _bench.AttackDelaySuppressed.Contains(creature);
}
