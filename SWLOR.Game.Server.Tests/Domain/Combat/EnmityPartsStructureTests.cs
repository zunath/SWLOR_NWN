using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// What the enmity split promises about its three parts: the table part takes no service and keeps the carried tables under their
/// keys, the change part owns the ten handlers, the attack part owns the one boot registration, no part holds a deferred
/// resolution, and the container hands out one instance of each.
/// </summary>
[TestFixture]
public class EnmityPartsStructureTests
{
    private const BindingFlags Declared = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    private static ParameterInfo[] Services(Type part) => part.GetConstructors().Single().GetParameters();

    [Test]
    public void TheTablePartTakesNoServiceTheChangePartEightAndTheAttackPartEight()
    {
        Services(typeof(EnmityService)).Should().BeEmpty();
        Services(typeof(EnmityChangeService)).Select(parameter => parameter.ParameterType).Should().Equal(
            typeof(IEnmityService), typeof(IEnmityAttackService), typeof(ICombatLeash), typeof(IHologramQuery), typeof(IPartyRoster),
            typeof(ICompanionLookup), typeof(IStatService), typeof(IStatusEffectService));
        Services(typeof(EnmityAttackService)).Select(parameter => parameter.ParameterType).Should().Equal(
            typeof(IEnmityService), typeof(ICombatLeash), typeof(IActivityService), typeof(IStealthService), typeof(ICompanionTargetQuery),
            typeof(ICombatActivityService), typeof(ICombatAttackTimingService), typeof(ICombatQueriesService));
    }

    [TestCase(typeof(EnmityService))]
    [TestCase(typeof(EnmityChangeService))]
    [TestCase(typeof(EnmityAttackService))]
    [TestCase(typeof(AIService))]
    [TestCase(typeof(StealthService))]
    public void NoServiceTheEnmitySplitTouchesTakesAnEnmityPartDeferred(Type service)
    {
        Services(service).Where(parameter => parameter.ParameterType.IsGenericType &&
                parameter.ParameterType.GetGenericArguments().Any(argument => argument.Name.StartsWith("IEnmity", StringComparison.Ordinal)))
            .Should().BeEmpty("the enmity parts are taken directly: the leash and stealth reach the attack part through their notices");
    }

    [Test]
    public void TheTablePartKeepsTheThreeCarriedTablesAndMarksTheAttackCommandMemoryRuntime()
    {
        var kinds = typeof(EnmityService).GetFields(Declared)
            .Where(field => field.GetCustomAttribute<ReloadStateAttribute>() != null)
            .ToDictionary(field => field.Name, field => field.GetCustomAttribute<ReloadStateAttribute>()!.Kind);

        kinds.Should().BeEquivalentTo(new Dictionary<string, ReloadStateKind>
        {
            ["_enemyEnmityTables"] = ReloadStateKind.Carried,
            ["_creatureToEnemies"] = ReloadStateKind.Carried,
            ["_proximityEnmityAmounts"] = ReloadStateKind.Carried,
            ["_attackCommandTimes"] = ReloadStateKind.Runtime,
            ["_attackCommandTargets"] = ReloadStateKind.Runtime,
        });
        typeof(EnmityChangeService).GetFields(Declared).Where(field => field.GetCustomAttribute<ReloadStateAttribute>()?.Kind == ReloadStateKind.Carried)
            .Should().BeEmpty("the carried tables stay in the table part, so nothing else is adopted");
        typeof(EnmityAttackService).GetFields(Declared).Where(field => field.GetCustomAttribute<ReloadStateAttribute>()?.Kind == ReloadStateKind.Carried)
            .Should().BeEmpty();
    }

    [Test]
    public void TheListFieldsOfTheTwoNoticesAreRuntimeState()
    {
        typeof(AIService).GetField("_retargetListeners", Declared)!.GetCustomAttribute<ReloadStateAttribute>()!.Kind
            .Should().Be(ReloadStateKind.Runtime);
        typeof(StealthService).GetField("_retargetListeners", Declared)!.GetCustomAttribute<ReloadStateAttribute>()!.Kind
            .Should().Be(ReloadStateKind.Runtime);
    }

    [Test]
    public void TheChangePartOwnsTheTenHandlersAndTheAttackPartOnlyTheBootRegistration()
    {
        static string[] Handlers(Type type) => type.GetMethods(Declared)
            .SelectMany(method => method.GetCustomAttributes<NWNEventHandler>().Select(handler => $"{method.Name}:{handler.Script}"))
            .OrderBy(name => name, StringComparer.Ordinal).ToArray();

        Handlers(typeof(EnmityService)).Should().BeEmpty("the table part owns no handler");
        Handlers(typeof(EnmityChangeService)).Should().Equal(new[]
        {
            $"CreatureAttacked:{ScriptName.OnCreatureAttackBefore}", $"CreatureDamaged:{ScriptName.OnCreatureDamagedBefore}",
            $"CreatureDeath:{ScriptName.OnCreatureDeathAfter}", $"CreatureDestroyed:{ScriptName.OnObjectDestroyed}",
            $"CreatureLimbo:{ScriptName.OnDMLimboBefore}", $"HealingCompleted:{ScriptName.OnHealAfter}",
            $"HealingStarted:{ScriptName.OnHealBefore}", $"PlayerDeath:{ScriptName.OnModuleDeath}",
            $"PlayerExit:{ScriptName.OnAreaExit}", $"PlayerExit:{ScriptName.OnModuleExit}",
        }.OrderBy(name => name, StringComparer.Ordinal));
        Handlers(typeof(EnmityAttackService)).Should().Equal($"RegisterRetargetListeners:{ScriptName.OnModuleCacheAfter}");

        var registration = typeof(EnmityAttackService).GetMethod(nameof(EnmityAttackService.RegisterRetargetListeners))!;
        registration.GetCustomAttribute<BootPhaseAttribute>()!.Reload.Should().Be(BootReload.Rerun);
        typeof(EnmityChangeService).GetMethod(nameof(EnmityChangeService.PlayerExit))!.GetCustomAttribute<PlayerSessionAttribute>()!.Reload
            .Should().Be(PlayerReload.LogoutOnly);
    }

    [Test]
    public void TheTablePartNoLongerDeclaresTheMembersThatMoved()
    {
        var moved = new[]
        {
            "CreatureDamaged", "CalculateDamageEnmity", "HealingStarted", "HealingCompleted", "CalculateHealingEnmity", "CreatureAttacked",
            "ModifyEnmity", "CalculateAdjustedEnmity", "TryTaunt", "CalculateTauntEnmity", "ModifyEnmityOnAll", "ReduceEnmity",
            "ReduceEnmityOnAll", "ModifyProximityEnmity", "RemoveCreatureEnmity", "CreatureDeath", "CreatureDestroyed", "PlayerDeath",
            "PlayerExit", "CreatureLimbo", "GetHighestEnmityAttackTarget", "IsEligibleAttackTarget", "ReevaluateEnemyAttackTargets",
            "AttackHighestEnmityTarget", "ResumeAttackAfterActionsCleared", "IssueAttackCommand",
        };

        typeof(EnmityService).GetMethods(Declared).Select(method => method.Name).Intersect(moved).Should().BeEmpty();
        typeof(IEnmityService).GetMethods().Select(method => method.Name).Intersect(moved).Should().BeEmpty();
    }

    [Test]
    public void TheContainerHandsOutOneInstanceOfEachPartAndTheReadsTheLowerDomainsOwn()
    {
        using var facades = new FacadeContainer();
        var table = facades.Get<IEnmityService>();

        table.Should().BeOfType<EnmityService>().And.BeSameAs(facades.Get<IEspionageEnmityQuery>());
        facades.Get<IEnmityChangeService>().Should().BeOfType<EnmityChangeService>().And.BeSameAs(facades.Get<IEnmityChangeService>());
        facades.Get<IEnmityAttackService>().Should().BeOfType<EnmityAttackService>().And.BeSameAs(facades.Get<IEnmityAttackService>());
        facades.Get<IHologramQuery>().Should().BeSameAs(facades.Get<IHoloComService>());
        typeof(EnmityChangeService).GetField("_table", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(facades.Get<IEnmityChangeService>())
            .Should().BeSameAs(table);
        typeof(EnmityAttackService).GetField("_table", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(facades.Get<IEnmityAttackService>())
            .Should().BeSameAs(table);
    }
}
