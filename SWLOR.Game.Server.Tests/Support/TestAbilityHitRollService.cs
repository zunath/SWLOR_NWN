using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The hit roll of the ability split (<c>Readmes/MaintainabilityRefactorAbilitySplit.md</c>, part 10) over the bench's observer,
/// melee technique, combat point and enmity stand-ins and its damage input, status application and message parts, with its calls
/// into the combat services answered by the bench, as <see cref="TestAbilityService"/> answered them before the move.
/// </summary>
public sealed class TestAbilityHitRollService : AbilityHitRollService
{
    private readonly AbilityBench _bench;

    public TestAbilityHitRollService(
        AbilityBench bench,
        IEnumerable<IAbilityImpactObserver> observers,
        IMeleeTechniqueService meleeTechnique,
        IAbilityDamageInputService damageInput,
        IAbilityStatusApplicationService statusApplication,
        IAbilityImpactMessageService impactMessage,
        ICombatPointService combatPoint,
        IEnmityService enmity)
        : base(observers, meleeTechnique, damageInput, statusApplication, impactMessage, combatPoint, enmity,
            FacadeForwarder.For<ICombatActivityService>(), FacadeForwarder.For<ICombatAbilityBonusService>(), FacadeForwarder.For<ICombatAbilityHitService>())
    {
        _bench = bench;
    }

    private void Log(string entry) => _bench.Log.Add(entry);

    protected override void TrackHostileAbilityActivity(uint creature) => Log($"TrackHostileActivity:{creature}");

    protected override void TrackHostileDefensiveCombatEntryActivity(uint creature, uint attacker) => Log($"TrackDefensiveEntry:{creature}:{attacker}");

    protected override int GetAbilityStatusCategoryHitChancePercentAdjustment(uint creature, SkillType skillType, StatusEffectCategory appliedCategories) => 0;

    protected override bool TryResolveAbilityHit(uint attacker, uint defender, SkillType skillType, PerkType perkType, out int hitRate, int hitChancePercentAdjustment, int skillLevelOverride, AbilityType statOverride, int accuracyFlatAdjustment)
    {
        Log($"ResolveHit:{hitChancePercentAdjustment}:{skillLevelOverride}:{statOverride}:{accuracyFlatAdjustment}");
        hitRate = _bench.HitRate;
        return _bench.HitResolves;
    }
}
