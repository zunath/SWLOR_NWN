using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Gameplay;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Tests.Composition;

/// <summary>
/// Proves that the accessor that replaces a removed facade costs what the facade did: reading a <see cref="GameServices"/>
/// property reads the same per-service slot (<c>FacadeCache</c>) and allocates nothing, so a call through it allocates
/// what the call on the instance allocates.
/// </summary>
[TestFixture]
public class GameServicesAllocationTests
{
    private const int Calls = 1000;

    private ContainerHost _host = null!;
    private IDisposable _scope = null!;

    [OneTimeSetUp]
    public void BuildContainer()
    {
        _host = new ContainerHost();
        CompositionRoot.Build(_host);
    }

    [SetUp]
    public void Install() => _scope = CompositionRoot.InstallForTests(_host.Provider);

    [TearDown]
    public void Remove() => _scope.Dispose();

    private static long Allocated(Action call)
    {
        for (var i = 0; i < 100; i++)
            call();

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < Calls; i++)
            call();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [Test]
    public void Reading_a_GameServices_property_allocates_nothing()
    {
        ITaxiService taxi = null!;
        IServerTaskService serverTask = null!;

        Allocated(() => taxi = GameServices.Taxi).Should().Be(0);
        Allocated(() => serverTask = GameServices.ServerTask).Should().Be(0);

        taxi.Should().BeSameAs(_host.Provider.GetRequiredService<ITaxiService>());
        serverTask.Should().BeSameAs(_host.Provider.GetRequiredService<IServerTaskService>());
    }

    /// <summary>The hot character services (P2-ZD): read on every swing and effect tick through the accessor.</summary>
    [Test]
    public void Reading_a_character_GameServices_property_allocates_nothing()
    {
        IStatService stat = null!;
        IStatusEffectService statusEffect = null!;
        ISkillService skill = null!;
        IPerkService perk = null!;

        Allocated(() => stat = GameServices.Stat).Should().Be(0);
        Allocated(() => statusEffect = GameServices.StatusEffect).Should().Be(0);
        Allocated(() => skill = GameServices.Skill).Should().Be(0);
        Allocated(() => perk = GameServices.Perk).Should().Be(0);

        stat.Should().BeSameAs(_host.Provider.GetRequiredService<IStatService>());
        statusEffect.Should().BeSameAs(_host.Provider.GetRequiredService<IStatusEffectService>());
        skill.Should().BeSameAs(_host.Provider.GetRequiredService<ISkillService>());
        perk.Should().BeSameAs(_host.Provider.GetRequiredService<IPerkService>());
    }

    [Test]
    public void Reading_the_database_through_GameServices_allocates_nothing()
    {
        // DB is read on many request paths (P2-ZI): the property and a read that does not reach Redis cost nothing more
        // than the instance.
        var instance = _host.Provider.GetRequiredService<IDBService>();
        IDBService db = null!;
        long version = -1;

        Allocated(() => db = GameServices.DB).Should().Be(0);
        var throughAccessor = Allocated(() => version = GameServices.DB.GetWriteVersion<Player>());
        var direct = Allocated(() => version = instance.GetWriteVersion<Player>());

        db.Should().BeSameAs(instance);
        throughAccessor.Should().Be(direct).And.Be(0);
        version.Should().Be(instance.GetWriteVersion<Player>());
    }

    [Test]
    public void The_recording_flag_every_managed_entry_reads_allocates_nothing()
    {
        // ManagedEntryDepth.Enter reads CombatRecorder.IsRecording on every managed entry point (main loop, script,
        // closure, signal): one read of the accessor's slot and one interface call, as the facade did.
        var instance = _host.Provider.GetRequiredService<ICombatRecorderService>();
        var recording = true;

        Allocated(() => recording = GameServices.CombatRecorder.IsRecording).Should().Be(0);
        GameServices.CombatRecorder.Should().BeSameAs(instance);
        recording.Should().Be(instance.IsRecording);
    }

    [Test]
    public void Reading_the_database_through_GameServices_allocates_nothing()
    {
        // DB is read on many request paths (P2-ZI): the property and a read that does not reach Redis cost nothing more
        // than the instance.
        var instance = _host.Provider.GetRequiredService<IDBService>();
        IDBService db = null!;
        long version = -1;

        Allocated(() => db = GameServices.DB).Should().Be(0);
        var throughAccessor = Allocated(() => version = GameServices.DB.GetWriteVersion<Player>());
        var direct = Allocated(() => version = instance.GetWriteVersion<Player>());

        db.Should().BeSameAs(instance);
        throughAccessor.Should().Be(direct).And.Be(0);
        version.Should().Be(instance.GetWriteVersion<Player>());
    }

    [Test]
    public void The_recording_flag_every_managed_entry_reads_allocates_nothing()
    {
        // ManagedEntryDepth.Enter reads CombatRecorder.IsRecording on every managed entry point (main loop, script,
        // closure, signal): one read of the accessor's slot and one interface call, as the facade did.
        var instance = _host.Provider.GetRequiredService<ICombatRecorderService>();
        var recording = true;

        Allocated(() => recording = GameServices.CombatRecorder.IsRecording).Should().Be(0);
        GameServices.CombatRecorder.Should().BeSameAs(instance);
        recording.Should().Be(instance.IsRecording);
    }

    [Test]
    public void A_call_through_GameServices_allocates_what_the_call_on_the_instance_allocates()
    {
        var instance = _host.Provider.GetRequiredService<IServerTaskService>();
        var now = new DateTime(2026, 11, 8, 12, 0, 0, DateTimeKind.Utc);
        var due = false;

        var throughAccessor = Allocated(() => due = GameServices.ServerTask.IsRestartReminderDue(now));
        var direct = Allocated(() => due = instance.IsRestartReminderDue(now));

        throughAccessor.Should().Be(direct).And.Be(0);
        due.Should().Be(instance.IsRestartReminderDue(now));
    }
}
