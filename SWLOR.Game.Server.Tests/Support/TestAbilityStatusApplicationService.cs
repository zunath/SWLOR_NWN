using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The status application of the ability split (<c>Readmes/MaintainabilityRefactorAbilitySplit.md</c>, part 18) over the bench's
/// status effect and resistance stand-ins and its impact state store, with the duration adjustment answered by the bench, as
/// <see cref="TestAbilityService"/> answered it before the move.
/// </summary>
public sealed class TestAbilityStatusApplicationService : AbilityStatusApplicationService
{
    private readonly AbilityBench _bench;

    public TestAbilityStatusApplicationService(AbilityBench bench, IStatusEffectService statusEffect, IStatusEffectApplicationService application, IResistanceService resistance, IAbilityImpactState impactState)
        : base(statusEffect, application, resistance, impactState, FacadeForwarder.For<ICombatAbilityBonusService>())
    {
        _bench = bench;
    }

    protected override int GetAbilityStatusDurationPercentAdjustment(uint creature, PerkType perkType, SkillType skillType, Type primaryStatusEffect, IEnumerable<Type> additionalStatusEffects, Func<IStatusEffect> statusEffectFactory) => _bench.StatusDurationAdjustment;
}
