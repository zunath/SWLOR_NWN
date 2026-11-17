using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Part 6 of the stat split, <c>CombatDeflectionReactionService</c> (Combat, P5-4d): what a deflection sets off, with its calls
/// into the other Combat services and the temporary stat modifiers answered by the bench (<c>StatBench.Seams.cs</c>). It reads
/// the stat adjustment and the pools through the bench's stat double, which it read on its own instance before the move, so a
/// test sees the same answers and the same log.
/// </summary>
public sealed class TestCombatDeflectionReactionService : CombatDeflectionReactionService
{
    private readonly StatBench _bench;

    public TestCombatDeflectionReactionService(
        StatBench bench,
        ICharacterRecording combatRecorder,
        IRecastService recast,
        IStatService stat)
        : base(combatRecorder, recast, FacadeForwarder.For<ICombatDamageTakenService>(), FacadeForwarder.For<ICombatAttackTimingService>(),
            FacadeForwarder.For<ICombatResourcesService>(), FacadeForwarder.For<ITemporaryStatModifierService>(), stat)
    {
        _bench = bench;
    }

    protected override uint GetNativeCreatureId(global::NWN.Native.API.CNWSCreature creature) => StatBench.PlayerCreature;

    protected override void TrackDeflection(uint creature, DeflectionSource source) => _bench.Log.Add($"TrackDeflection:{source}");

    protected override bool TryUseStatTrigger(uint creature, StatType statType, int cooldownSeconds)
    {
        _bench.Log.Add($"TryTrigger:{statType}:{cooldownSeconds}");
        return _bench.StatTriggerAvailable;
    }

    protected override void ReplaceTemporaryStatModifier(uint creature, StatType statType, int amount, float durationSeconds, StatType groupStatType) =>
        _bench.Log.Add($"Temporary:{statType}:{amount}:{durationSeconds}:{groupStatType}");

    protected override void GrantNextSkillAbilityBonuses(uint creature, SkillType skillType, int damageBonus, int criticalRate, int durationSeconds) =>
        _bench.Log.Add($"NextSkillBonuses:{skillType}:{damageBonus}:{criticalRate}:{durationSeconds}");

    protected override void GrantNextAutoAttackCriticalRateBonus(uint creature, SkillType skillType, int criticalRate, int durationSeconds) =>
        _bench.Log.Add($"NextAutoAttackCritical:{skillType}:{criticalRate}:{durationSeconds}");

    protected override void GrantNextAbilityNoDelay(uint creature, SkillType skillType, int durationSeconds) =>
        _bench.Log.Add($"NextAbilityNoDelay:{skillType}:{durationSeconds}");
}
