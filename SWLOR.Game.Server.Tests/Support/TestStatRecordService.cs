using System.Collections.Immutable;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Part 3 of the stat split, <c>StatRecordService</c> (P5-4e): the gear-fed player records and the Combat Readiness equipment,
/// with its defense damage type seam answered. It reads the pools through the bench's stat double.
/// </summary>
public sealed class TestStatRecordService : StatRecordService
{
    public TestStatRecordService(IDBService db, IStatService stat)
        : base(db, stat)
    {
    }

    protected override ImmutableArray<CombatDamageType> GetDefenseDamageTypes() => ImmutableArray.Create(CombatDamageType.Physical, CombatDamageType.Force);
}
