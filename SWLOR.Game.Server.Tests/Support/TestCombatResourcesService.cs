using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Part 7 of the stat split, the healing adjustments that joined <c>CombatResourcesService</c> (Combat, P5-4d), with the stat
/// trigger and the temporary stat modifier answered by the bench (<c>StatBench.Seams.cs</c>), as the stat double's seams
/// answered them before the move. It reads the stat adjustment and the pools through the bench's stat double, so a healing
/// test sees the same answers and the same log.
/// </summary>
public sealed class TestCombatResourcesService : CombatResourcesService
{
    private readonly StatBench _bench;

    public TestCombatResourcesService(
        StatBench bench,
        IStatService stat,
        IStatusEffectApplicationService statusEffect,
        ICombatRecorderService combatRecorder,
        IPvpCombatService pvpCombat,
        ICompanionLookup companions)
        : base(FacadeForwarder.For<ICombatQueriesService>(), FacadeForwarder.For<IAbilityImpactState>(), stat, statusEffect, combatRecorder,
            FacadeForwarder.For<ITemporaryStatModifierService>(), pvpCombat, companions)
    {
        _bench = bench;
    }

    public override bool TryUseStatTrigger(uint creature, StatType statType, int cooldownSeconds)
    {
        _bench.Log.Add($"TryTrigger:{statType}:{cooldownSeconds}");
        return _bench.StatTriggerAvailable;
    }

    protected override void ReplaceTemporaryStatModifier(uint creature, StatType statType, int amount, float durationSeconds, StatType groupStatType) =>
        _bench.Log.Add($"Temporary:{statType}:{amount}:{durationSeconds}:{groupStatType}");
}
