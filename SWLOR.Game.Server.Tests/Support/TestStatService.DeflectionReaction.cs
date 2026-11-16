using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

// Part 6, CombatDeflectionReactionService (Combat): what a deflection triggers.
public sealed partial class TestStatService
{
    protected override uint GetNativeCreatureId(global::NWN.Native.API.CNWSCreature creature) => StatBench.PlayerCreature;

    protected override void TrackDeflection(uint creature, DeflectionSource source) => _bench.Log.Add($"TrackDeflection:{source}");

    protected override void GrantNextSkillAbilityBonuses(uint creature, SkillType skillType, int damageBonus, int criticalRate, int durationSeconds) =>
        _bench.Log.Add($"NextSkillBonuses:{skillType}:{damageBonus}:{criticalRate}:{durationSeconds}");

    protected override void GrantNextAutoAttackCriticalRateBonus(uint creature, SkillType skillType, int criticalRate, int durationSeconds) =>
        _bench.Log.Add($"NextAutoAttackCritical:{skillType}:{criticalRate}:{durationSeconds}");

    protected override void GrantNextAbilityNoDelay(uint creature, SkillType skillType, int durationSeconds) =>
        _bench.Log.Add($"NextAbilityNoDelay:{skillType}:{durationSeconds}");
}
