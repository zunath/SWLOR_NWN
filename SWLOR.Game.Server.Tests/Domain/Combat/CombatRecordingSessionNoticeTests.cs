using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The notice the recorder sends its listeners when a recording starts and stops. The damage part is the one listener: it
/// empties its grants and death frames where <c>Start</c> and <c>Stop</c> used to clear them, so no damage source or death
/// frame carries into the next recording.
/// </summary>
[TestFixture]
public class CombatRecordingSessionNoticeTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint AreaOne = 100;
    private const uint Alpha = 1;
    private const uint Beta = 2;

    private sealed class Listener : ICombatRecordingSessionListener
    {
        private readonly RecorderProbe _recorder;
        private readonly List<string> _log;

        public Listener(RecorderProbe recorder, List<string> log)
        {
            _recorder = recorder;
            _log = log;
        }

        public void OnRecordingStarted() =>
            _log.Add($"started recording={_recorder.IsRecording} participants={_recorder.Core.Participants.Count} " +
                     $"events={_recorder.Core.EventsEnqueued} writer={(_recorder.Core.Writer == null ? "none" : _recorder.Core.Writer == _recorder.LastWriter ? "last" : "other")}");

        public void OnRecordingStopped() =>
            _log.Add($"stopped recording={_recorder.IsRecording} participants={_recorder.Core.Participants.Count} " +
                     $"writer={(_recorder.Core.Writer == _recorder.LastWriter ? "last" : "other")}");
    }

    private readonly List<IDisposable> _scopes = new();
    private string _directory = null!;
    private RecorderProbe _recorder = null!;
    private FacadeContainer _facades = null!;
    private uint _self = ObjectInvalid;

    [SetUp]
    public void SetUp()
    {
        var engine = new FakeNWScript();
        engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != ObjectInvalid && (uint)args[0]! != 0);
        engine.SetReturnHandler("GetArea", args => (uint)args[0]! == ObjectInvalid ? ObjectInvalid : AreaOne);
        engine.SetReturnHandler("GetObjectType", _ => ObjectType.Creature);
        engine.SetReturnHandler("GetName", args => $"name-{(uint)args[0]!}");
        engine.SetReturnHandler("GetResRef", args => $"res-{(uint)args[0]!}");
        engine.SetReturn("GetIsPC", false);
        engine.SetReturnHandler("GetFirstPC", _ => ObjectInvalid);
        engine.SetReturnHandler("OBJECT_SELF", _ => _self);
        engine.SetReturnHandler("GetLastKiller", _ => ObjectInvalid);
        _scopes.Add(NWScriptRuntime.Override(engine));

        _directory = Path.Combine(Path.GetTempPath(), "mr-p5-7j3-notice-" + Guid.NewGuid().ToString("N"));
        var presence = ScriptedProxy.Create<IDMPresenceService>(out var presenceScript);
        presenceScript.On("IsControlledByDM", _ => false);
        _recorder = new RecorderProbe(presence, new InMemoryDBService(), _directory + "/", registerDamageListener: false);
        _facades = new FacadeContainer(services =>
        {
            services.Replace(ServiceDescriptor.Singleton<ICombatRecordSession>(_recorder.Core));
            services.Replace(ServiceDescriptor.Singleton<ICombatRecorderService>(_recorder));
            services.Replace(ServiceDescriptor.Singleton<ICombatRecordAbilityService>(_recorder.Ability));
            services.Replace(ServiceDescriptor.Singleton<ICombatRecordDamageService>(_recorder.Damage));
        });
        _scopes.Add(_facades.Install());
    }

    [TearDown]
    public void TearDown()
    {
        if (_recorder.IsRecording)
            _recorder.Stop("teardown", out _);
        try
        {
            _recorder.LastWriter?.Completion.Wait(TimeSpan.FromSeconds(10));
        }
        catch (AggregateException)
        {
        }

        foreach (var scope in _scopes)
            scope.Dispose();
        _scopes.Clear();
        _facades.Dispose();
        try
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, true);
        }
        catch (IOException)
        {
        }
    }

    private List<JsonElement> StopAndRead()
    {
        if (_recorder.IsRecording)
            _recorder.Stop("test", out _);
        var writer = _recorder.LastWriter!;
        writer.Completion.Wait(TimeSpan.FromSeconds(10)).Should().BeTrue();
        return File.ReadAllLines(writer.EventsPath).Select(line => JsonDocument.Parse(line).RootElement.Clone()).ToList();
    }

    private static List<JsonElement> Of(List<JsonElement> events, string type) =>
        events.Where(e => e.GetProperty("type").GetString() == type).ToList();

    [Test]
    public void StartWithNoListenerRegisteredThrows()
    {
        var act = () => _recorder.Start(AreaOne, "x", out _);

        act.Should().Throw<InvalidOperationException>().WithMessage("*listener*");
    }

    [Test]
    public void StopWithNoListenerRegisteredThrows()
    {
        _recorder.RegisterSessionListener(_recorder.Damage);
        _recorder.Start(AreaOne, "x", out _).Should().BeTrue();
        var listeners = (List<ICombatRecordingSessionListener>)typeof(CombatRecorderService)
            .GetField("_sessionListeners", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(_recorder)!;
        listeners.Clear();

        var act = () => _recorder.Stop("x", out _);

        act.Should().Throw<InvalidOperationException>().WithMessage("*listener*");
    }

    [Test]
    public void EachListenerIsToldOnceOnStartAndOnceOnStopInRegistrationOrder()
    {
        var log = new List<string>();
        var first = new Listener(_recorder, log);
        var second = new Listener(_recorder, log);
        _recorder.RegisterSessionListener(first);
        _recorder.RegisterSessionListener(second);

        _recorder.Start(AreaOne, "x", out _).Should().BeTrue();
        _recorder.Stop("x", out _).Should().BeTrue();

        log.Select(line => line.Split(' ')[0]).Should().Equal("started", "started", "stopped", "stopped");
    }

    [Test]
    public void ARepeatRegistrationKeepsOneEntry()
    {
        var log = new List<string>();
        var listener = new Listener(_recorder, log);
        _recorder.RegisterSessionListener(listener);
        _recorder.RegisterSessionListener(listener);
        _recorder.RegisterSessionListener(_recorder.Damage);
        _recorder.RegisterSessionListener(_recorder.Damage);

        _recorder.Start(AreaOne, "x", out _).Should().BeTrue();
        _recorder.Stop("x", out _).Should().BeTrue();

        log.Should().HaveCount(2, "one start notice and one stop notice for the one entry");
        var listeners = (List<ICombatRecordingSessionListener>)typeof(CombatRecorderService)
            .GetField("_sessionListeners", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(_recorder)!;
        listeners.Should().HaveCount(2);
    }

    [Test]
    public void TheRegistrationHandlerOfThePartKeepsOneEntryWhenARerunRepeatsIt()
    {
        var damage = _recorder.Damage;

        damage.RegisterSessionListener();
        damage.RegisterSessionListener();

        var listeners = (List<ICombatRecordingSessionListener>)typeof(CombatRecorderService)
            .GetField("_sessionListeners", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(_recorder)!;
        listeners.Should().Equal(damage);
    }

    [Test]
    public void StartFiresTheNoticeWhereTheTablesWereClearedAfterTheResetAndBeforeTheWriterIsMade()
    {
        var log = new List<string>();
        _recorder.RegisterSessionListener(new Listener(_recorder, log));
        _recorder.RegisterSessionListener(_recorder.Damage);

        _recorder.Start(AreaOne, "first", out _).Should().BeTrue();
        _recorder.Stop("x", out _);
        _recorder.LastWriter!.Completion.Wait(TimeSpan.FromSeconds(10));
        var firstWriter = _recorder.LastWriter;
        _recorder.Start(AreaOne, "second", out _).Should().BeTrue();

        log[0].Should().Be("started recording=False participants=0 events=0 writer=none", "no writer exists before the first recording");
        log[2].Should().Be("started recording=False participants=0 events=0 writer=last",
            "after a stop the session holds the last writer, which the second start replaces after the notice");
        _recorder.LastWriter.Should().NotBeSameAs(firstWriter);
    }

    [Test]
    public void StopFiresTheNoticeAfterTheRecordingFlagIsOffTheWriterIsCompletedAndTheSessionIsReset()
    {
        var log = new List<string>();
        _recorder.RegisterSessionListener(new Listener(_recorder, log));
        _recorder.RegisterSessionListener(_recorder.Damage);
        _recorder.Start(AreaOne, "x", out _).Should().BeTrue();
        _recorder.Core.GetParticipantId(Alpha);

        _recorder.Stop("x", out _);

        log[1].Should().Be("stopped recording=False participants=0 writer=last",
            "the flag is off and the participants were cleared by the session reset before the notice");
    }

    [Test]
    public void ATemporaryHitPointGrantOfOneRecordingIsNotCreditedInTheNext()
    {
        _recorder.RegisterSessionListener(_recorder.Damage);
        _recorder.Start(AreaOne, "first", out _).Should().BeTrue();
        using (_recorder.BeginAbilityAttribution(Beta, new AbilityDetail { Name = "Strike", AbilityLevel = 3 }))
            _recorder.Damage.RecordTemporaryHitPointsGranted(Beta, Beta, "shield", 30, 5f);
        StopAndRead();

        _recorder.Start(AreaOne, "second", out _).Should().BeTrue();
        _recorder.Damage.RecordTemporaryHitPointsTrimmed(Beta, "shield", 10, 20);
        var trim = Of(StopAndRead(), "temp_hp_trim").Single();

        trim.GetProperty("granted_kind").GetString().Should().Be("unattributed", "the grant table started empty");
    }

    [Test]
    public void ADeathFrameOfOneRecordingDoesNotHideADeathInTheSameFrameOfTheNext()
    {
        _recorder.RegisterSessionListener(_recorder.Damage);
        _recorder.Start(AreaOne, "first", out _).Should().BeTrue();
        _self = Beta;
        _recorder.Frame = 7;
        _recorder.Damage.RecordCreatureDeath();
        StopAndRead();

        _recorder.Start(AreaOne, "second", out _).Should().BeTrue();
        _recorder.Frame = 7;
        _recorder.Damage.RecordCreatureDeath();
        _recorder.Damage.RecordCreatureDeath();

        Of(StopAndRead(), "death").Should().ContainSingle("the second recording's own death is recorded once, not hidden by the first one's frame");
    }
}
