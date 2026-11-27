using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// What the death split promises about its two parts: the death service keeps the three handlers it had and takes ten services (the
/// map's limit), the respawn part takes four and owns the login handler, neither takes a service deferred, and the container hands out
/// one instance of each, with the death service holding the respawn part's instance.
/// </summary>
[TestFixture]
public class DeathPartsStructureTests
{
    private const BindingFlags Declared = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    private static ParameterInfo[] Services(Type part) => part.GetConstructors().Single().GetParameters();

    private static IEnumerable<string> Handlers(Type part) => part.GetMethods(Declared)
        .SelectMany(method => method.GetCustomAttributes<NWNEventHandler>().Select(handler => $"{method.Name}:{handler.Script}"))
        .OrderBy(name => name, StringComparer.Ordinal);

    [Test]
    public void TheDeathServiceTakesTenServicesAndTheRespawnPartFour()
    {
        Services(typeof(DeathService)).Select(parameter => parameter.ParameterType).Should().Equal(
            typeof(IDBService), typeof(INamedAnimationService), typeof(IMessagingService), typeof(IPlayerNameService),
            typeof(IStatusEffectService), typeof(IStatusEffectApplicationService), typeof(ICombatRecorderService),
            typeof(ICombatRecordDamageService), typeof(IEnumerable<ICreatureRevivedListener>), typeof(ICombatDamageTakenService),
            typeof(IDeathRespawnService));
        Services(typeof(DeathRespawnService)).Select(parameter => parameter.ParameterType).Should().Equal(
            typeof(IDBService), typeof(IAreaLookup), typeof(ICityUpgradeLookup), typeof(IPlayerNameService));
    }

    [TestCase(typeof(DeathService))]
    [TestCase(typeof(DeathRespawnService))]
    public void NeitherDeathPartTakesAServiceDeferred(Type part)
    {
        Services(part).Where(parameter => parameter.ParameterType.IsGenericType &&
                (parameter.ParameterType.GetGenericTypeDefinition() == typeof(Lazy<>) || parameter.ParameterType.GetGenericTypeDefinition() == typeof(Func<>)))
            .Should().BeEmpty();
    }

    [Test]
    public void TheHandlersStayWhereTheyWereAndTheLoginHandlerIsTheRespawnPartsOwn()
    {
        Handlers(typeof(DeathService)).Should().Equal(
            $"OnPlayerDeath:{ScriptName.OnModuleDeath}", $"OnPlayerDying:{ScriptName.OnModuleDying}", $"OnPlayerRespawn:{ScriptName.OnModuleRespawn}");
        Handlers(typeof(DeathRespawnService)).Should().Equal($"InitializeRespawnPoint:{ScriptName.OnModuleEnter}");
    }

    [Test]
    public void TheDeathServiceNoLongerDeclaresTheMembersThatMoved()
    {
        var moved = new[] { "InitializeRespawnPoint", "GetRespawnLocation", "HasRespawnArea", "IsWalkable", "SendToHomePoint", "ApplyPenalties", "WriteAudit" };

        typeof(DeathService).GetMethods(Declared).Select(method => method.Name).Intersect(moved).Should().BeEmpty();
        typeof(IDeathService).GetMethods().Select(method => method.Name).Intersect(moved).Should().BeEmpty();
    }

    [Test]
    public void TheContainerHandsOutOneInstanceOfEachPartAndTheDeathServiceHoldsTheRespawnPartsInstance()
    {
        using var facades = new FacadeContainer();
        var respawn = facades.Get<IDeathRespawnService>();

        respawn.Should().BeOfType<DeathRespawnService>().And.BeSameAs(facades.Get<IDeathRespawnService>());
        facades.Get<IDeathService>().Should().BeOfType<DeathService>().And.BeSameAs(facades.Get<IDeathService>());
        typeof(DeathService).GetField("_respawn", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(facades.Get<IDeathService>())
            .Should().BeSameAs(respawn);
    }
}
