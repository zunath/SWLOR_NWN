using System.Collections.Immutable;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

// Part 3, StatRecordService: the gear-fed player records and the Combat Readiness equipment.
public sealed partial class TestStatService
{
    protected override ImmutableArray<CombatDamageType> GetDefenseDamageTypes() => ImmutableArray.Create(CombatDamageType.Physical, CombatDamageType.Force);
}
