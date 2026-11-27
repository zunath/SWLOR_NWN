using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Part 2 of the stat split, <c>CreatureStatService</c>: movement, maximum HP and food HP, attributes, NPC loading and
/// regeneration, with its calls into unconverted code answered by the bench (<c>StatBench.Seams.cs</c>). It reads the stat
/// adjustment and the pools through the bench's stat double, which it read on its own instance before the move, so a test sees
/// the same answers and the same log.
/// </summary>
public sealed class TestCreatureStatService : CreatureStatService
{
    private readonly StatBench _bench;

    public TestCreatureStatService(
        StatBench bench,
        IDBService db,
        ICharacterRecording combatRecorder,
        ICompanionLookup companions,
        IStatusEffectService statusEffect,
        IStatService stat)
        : base(db, combatRecorder, companions, statusEffect, stat)
    {
        _bench = bench;
    }

    protected override void ScheduleTemporaryHitPointLimitCheck(uint creature) => _bench.Log.Add($"LimitCheck:{creature}");

    protected override bool IsDroid(uint creature) => _bench.Droids.Contains(creature);
}
