using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Part 2 of the stat split, <c>CreatureStatService</c>, with its calls into unconverted code answered without allocating, as
/// <see cref="AllocationStatService"/> answered them before the part moved.
/// </summary>
internal sealed class AllocationCreatureStatService : CreatureStatService
{
    public AllocationCreatureStatService(
        IDBService db,
        ICharacterRecording combatRecorder,
        ICompanionLookup companions,
        IStatusEffectService statusEffect,
        IStatService stat)
        : base(db, combatRecorder, companions, statusEffect, stat)
    {
    }

    protected override bool IsDroid(uint creature) => false;
}
