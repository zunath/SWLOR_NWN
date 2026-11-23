#nullable enable
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The companion target authority (part CC-4) with the clock, the weapon range, the perk, the feat and the ability use
/// answered by the test.
/// </summary>
public sealed class TestCompanionTargetAuthorityService : CompanionTargetAuthorityService
{
    private readonly CompanionTestState _state;

    public TestCompanionTargetAuthorityService(
        ICompanionControlStoreService store,
        ICompanionPositioningService positioning,
        CompanionTestState state)
        : base(
            store,
            positioning,
            FacadeForwarder.For<IAbilityActivationService>(),
            FacadeForwarder.For<IAbilityCatalog>(),
            FacadeForwarder.For<IPerkService>(),
            FacadeForwarder.For<ICombatActivityService>(),
            FacadeForwarder.For<ICombatQueriesService>())
    {
        _state = state;
    }

    protected override DateTime UtcNow() => _state.Now;

    protected override DateTime GetLastOffensiveActivityAt(uint creature) =>
        _state.OffensiveAt.GetValueOrDefault(creature);

    protected override SkillType GetEquippedWeaponSkillType(uint creature) => SkillType.Invalid;

    protected override float GetWeaponEngagementRange(SkillType skill) => _state.WeaponRange;

    protected override int GetPerkLevel(uint creature, PerkType perkType) => _state.PerkLevels.GetValueOrDefault(perkType);

    protected override bool IsFeatRegistered(FeatType feat) => _state.Registered.ContainsKey(feat);

    protected override AbilityDetail GetAbilityDetail(FeatType feat)
    {
        _state.AbilityLookups++;
        return _state.Registered[feat];
    }

    protected override bool CanUseAbility(uint activator, uint target, FeatType feat, int effectiveLevel, Location targetLocation)
    {
        _state.UsedLevels.Add(effectiveLevel);
        return _state.UsableFeats.Contains(feat);
    }
}
