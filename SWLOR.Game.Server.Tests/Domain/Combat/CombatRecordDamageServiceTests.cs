using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The damage records part of the recorder: what it holds, where its state lives and what a reload does with it. The records
/// themselves (what each event writes) are pinned by <see cref="CombatRecorderServiceTests"/> and the recorder transcript, which
/// call this part; the session notices are pinned by <see cref="CombatRecordingSessionNoticeTests"/>.
/// </summary>
[TestFixture]
public class CombatRecordDamageServiceTests
{
    private const BindingFlags Instance = BindingFlags.NonPublic | BindingFlags.Instance;

    [Test]
    public void ItHoldsTheSessionAndTheRecorderAndNothingElse()
    {
        var constructor = typeof(CombatRecordDamageService).GetConstructors().Should().ContainSingle().Subject;

        constructor.GetParameters().Select(parameter => parameter.ParameterType).Should().Equal(
            typeof(ICombatRecordSession), typeof(ICombatRecorderService));
        constructor.GetParameters().Should().NotContain(parameter => parameter.ParameterType.IsGenericType, "no Lazy<> is added");
    }

    [Test]
    public void ItsThreeTablesAreTwoNowThatTheLastDamageSourcesLiveInTheSessionCore()
    {
        // P5-7j1 moved _lastDamageSources into the core, which Reset clears; the part keeps the other two tables.
        typeof(CombatRecordDamageService).GetFields(Instance).Where(field => !field.IsStatic && field.FieldType.IsGenericType &&
                field.FieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            .Select(field => field.Name).OrderBy(name => name).Should().Equal("_deathFrames", "_temporaryHitPointGrants");
    }

    [Test]
    public void EveryTableTheBootHandlerOwnsIsRuntimeStateThatAReloadRebuilds()
    {
        foreach (var name in new[] { "_deathFrames", "_temporaryHitPointGrants" })
        {
            typeof(CombatRecordDamageService).GetField(name, Instance)!.GetCustomAttribute<ReloadStateAttribute>()!.Kind
                .Should().Be(ReloadStateKind.Runtime, name);
        }

        typeof(CombatRecorderService).GetField("_sessionListeners", Instance)!.GetCustomAttribute<ReloadStateAttribute>()!.Kind
            .Should().Be(ReloadStateKind.Runtime);
    }

    [Test]
    public void ItRegistersFromARerunHandlerOnModuleCacheBefore()
    {
        var handler = typeof(CombatRecordDamageService).GetMethod(nameof(CombatRecordDamageService.RegisterSessionListener), Type.EmptyTypes)!;

        handler.GetCustomAttribute<BootPhaseAttribute>()!.Reload.Should().Be(BootReload.Rerun);
        handler.GetCustomAttributes<NWNEventHandler>().Select(attribute => attribute.Script).Should().Equal(ScriptName.OnModuleCacheBefore);
    }

    [Test]
    public void TheDeathHandlersMovedWithTheirEvents()
    {
        typeof(CombatRecordDamageService).GetMethod(nameof(CombatRecordDamageService.RecordCreatureDeath))!
            .GetCustomAttributes<NWNEventHandler>().Select(attribute => attribute.Script).Should().Equal(ScriptName.OnCreatureDeathBefore);
        typeof(CombatRecordDamageService).GetMethod(nameof(CombatRecordDamageService.RecordPlayerDeath))!
            .GetCustomAttributes<NWNEventHandler>().Select(attribute => attribute.Script).Should().Equal(ScriptName.OnModuleDeath);
        typeof(CombatRecorderService).GetMethods().SelectMany(method => method.GetCustomAttributes<NWNEventHandler>())
            .Select(attribute => attribute.Script).Should().Equal(ScriptName.OnSWLORApplicationShutdown);
    }

    [Test]
    public void TheRecorderNoLongerDeclaresTheMovedMembersOrTheirTables()
    {
        var moved = new[]
        {
            "RecordDamageApplied", "RecordHealingApplied", "RecordTemporaryHitPointPoolRemoving", "RecordTemporaryHitPointsTrimmed",
            "RecordTemporaryHitPointsGranted", "RecordCreatureDeath", "RecordPlayerDeath", "RecordRespawn", "KeepLethalHitPoints",
            "RecordResurrection", "RecordSubdual"
        };
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        typeof(ICombatRecorderService).GetMethods().Select(method => method.Name).Should().NotIntersectWith(moved);
        typeof(CombatRecorderService).GetMethods(All).Select(method => method.Name).Should().NotIntersectWith(moved);
        typeof(ICombatRecordDamageService).GetMethods().Select(method => method.Name).Should().BeEquivalentTo(moved);
        typeof(CombatRecorderService).GetFields(All).Select(field => field.Name)
            .Should().NotContain(new[] { "_temporaryHitPointGrants", "_deathFrames" });
    }

    [Test]
    public void TheContainerHoldsOnePartThatIsNotTheRecorderAndTheRecorderKnowsItAsAListener()
    {
        using var facades = new FacadeContainer();
        using var installed = facades.Install();

        var part = facades.Get<ICombatRecordDamageService>();

        part.Should().BeOfType<CombatRecordDamageService>();
        part.Should().NotBeSameAs(facades.Get<ICombatRecorderService>());
        GameServices.CombatRecordDamage.Should().BeSameAs(part);
        facades.Get<ICombatRecordDamageService>().Should().BeSameAs(part, "it is one singleton");
        part.Should().BeAssignableTo<ICombatRecordingSessionListener>();
    }
}
