using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Part 3 of the status effect split, <c>StatusEffectApplicationService</c>, with the active ability impact answered by the bench.
/// The notices an application sends reach the listener stand-ins the bench registers on the tracker part.
/// </summary>
public sealed class TestStatusEffectApplicationService : StatusEffectApplicationService
{
    private readonly StatusEffectBench _bench;

    public TestStatusEffectApplicationService(
        StatusEffectBench bench,
        IStatusEffectService statusEffect,
        IStatusEffectCatalogService statusCatalog,
        IMessagingService messaging,
        IPlayerNameService playerName,
        IStatusEffectDurationService duration,
        IStatusEffectCombatEventService combatEvents)
        : base(statusEffect, statusCatalog, messaging, playerName, FacadeForwarder.For<IActiveAbilityImpact>(), duration, combatEvents)
    {
        _bench = bench;
    }

    protected override AbilityImpactSummary GetActiveAbilityImpactSummary(uint activator) => _bench.ActiveImpact!;
}
