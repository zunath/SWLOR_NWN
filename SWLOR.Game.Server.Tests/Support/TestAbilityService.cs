using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The part of the ability split that keeps the combat impacts content calls (<c>Readmes/MaintainabilityRefactorAbilitySplit.md</c>,
/// part 9), over the bench's impact state store, its lifecycle, per-target, message and visual parts and the geometry, telegraph
/// and animation stand-ins, with its calls into the combat services answered by the bench. Every other part has its own double
/// (<c>TestAbility*Service</c>); the answers are properties of <see cref="AbilityBench"/>, so the tests that set them do not change.
/// </summary>
public sealed class TestAbilityService : AbilityService
{
    private readonly AbilityBench _bench;

    public TestAbilityService(
        AbilityBench bench,
        IAbilityImpactState impactState,
        IAbilityImpactLifecycleService lifecycle,
        IAbilityTargetImpactService targetImpact,
        IAbilityImpactMessageService impactMessage,
        IAbilityImpactVisualService impactVisual,
        ICombatImpactGeometryService geometry,
        ITelegraphService telegraph,
        INamedAnimationService namedAnimation)
        : base(impactState, lifecycle, targetImpact, impactMessage, impactVisual, geometry, telegraph, namedAnimation,
            FacadeForwarder.For<ICombatResourcesService>(), FacadeForwarder.For<ICombatAbilityHitRidersService>())
    {
        _bench = bench;
    }

    private void Log(string entry) => _bench.Log.Add(entry);

    // The impact lifecycle answers the same through its own (TestAbilityImpactLifecycleService).
    protected override void ApplyAbilityImpactEffects(uint activator, AbilityImpactSummary summary) =>
        Log($"ApplyAbilityImpactEffects:{activator}:{summary.ImpactedTargetCount}");

    protected override void DeferAbilityStaminaCostContext(uint creature, AbilityDetail ability) =>
        Log($"DeferStamina:{creature}:{ability?.Name}");

    protected override void CompleteDeferredAbilityStaminaCostContext(uint creature, AbilityDetail ability) =>
        Log($"CompleteDeferredStamina:{creature}:{ability?.Name}");
}
