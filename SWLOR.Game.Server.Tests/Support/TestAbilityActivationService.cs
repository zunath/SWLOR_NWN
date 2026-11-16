using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The activation checks of the ability split (<c>Readmes/MaintainabilityRefactorAbilitySplit.md</c>, part 6) over the real
/// target checks (part 7), with their calls into other services answered by the bench, as <see cref="TestAbilityService"/>
/// answered them before the move.
/// </summary>
public sealed class TestAbilityActivationService : AbilityActivationService
{
    private readonly AbilityBench _bench;

    public TestAbilityActivationService(AbilityBench bench, IAbilityCatalog catalog, IMimicryService mimicry, IActivityService activity, IAbilityTargetCheckService targetCheck)
        : base(catalog, FacadeForwarder.For<ISpaceModeQuery>(), FacadeForwarder.For<IPerkService>(), mimicry, activity, FacadeForwarder.For<IStatService>(),
            FacadeForwarder.For<ICombatQueriesService>(), targetCheck)
    {
        _bench = bench;
    }

    protected override string GetUnusableEquipmentError(uint creature) => _bench.EquipmentError;

    // The Force power rule moved to Combat (CombatRules and CombatQueriesService.IsForcePower); the check keeps the skill the
    // doubles answer for an ability (TestAbilityService.Shared.cs: GetAbilitySkillType is the ability's own skill).
    protected override bool IsForcePower(uint creature, AbilityDetail ability) => CombatRules.IsForcePower(ability.IsForcePower, ability.SkillType);

    protected override bool IsPlayerInSpaceMode(uint player) => _bench.SpaceMode.Contains(player);

    protected override bool ShouldEnforceActiveAbilityFeatReplacement(uint creature, PerkType perkType) => _bench.EnforcesFeatReplacement;

    protected override bool IsCurrentActiveAbilityFeat(FeatType feat, PerkType perkType, int perkLevel) => _bench.RepliesActiveFeat;

    protected override bool HasEquippedWeaponForAbility(uint creature) => _bench.HasEquippedWeapon;

    // Dark Force and the damage formulas answer the same read through TestAbilityService.Shared.cs, the aura part through
    // TestAbilityAuraService.
    protected override int GetStatAdjustment(uint creature, StatType stat) => _bench.Stats.GetValueOrDefault((creature, stat));

    // The damage formulas and the weapon trait reads answer the same through TestAbilityService.Shared.cs.
    protected override bool IsWeaponSkillType(SkillType skillType) => _bench.WeaponSkills.Contains(skillType);
}
