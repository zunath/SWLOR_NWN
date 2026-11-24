using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The session core on its own: the queue and held events, the damage and heal application scopes, the participants and the
/// recorded area, with the real writer and a recording fake engine. The recorder's own behaviour is in
/// <c>CombatRecorderServiceTests</c> and <c>CombatRecorderTranscriptTests</c>; these tests need no recorder.
/// </summary>
[TestFixture]
public class CombatRecordSessionCoreTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint AreaOne = 100;
    private const uint AreaTwo = 200;
    private const uint Alpha = 1;
    private const uint Beta = 2;
    private const uint Outsider = 4;
    private const uint Item = 50;

    private readonly List<IDisposable> _scopes = new();
    private readonly Dictionary<uint, uint> _areaOf = new();
    private string _directory = null!;
    private RecorderProbeCore _core = null!;

    [SetUp]
    public void SetUp()
    {
        _areaOf.Clear();
        foreach (var creature in new[] { Alpha, Beta })
            _areaOf[creature] = AreaOne;
        _areaOf[Outsider] = AreaTwo;
        _areaOf[Item] = AreaOne;

        var engine = new FakeNWScript();
        engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != ObjectInvalid && (uint)args[0]! != 0);
        engine.SetReturnHandler("GetArea", args => _areaOf.GetValueOrDefault((uint)args[0]!, ObjectInvalid));
        engine.SetReturnHandler("GetObjectType", args => (uint)args[0]! == Item ? ObjectType.Item : ObjectType.Creature);
        engine.SetReturnHandler("GetName", args => $"name-{(uint)args[0]!}");
        engine.SetReturnHandler("GetResRef", args => $"res-{(uint)args[0]!}");
        engine.SetReturn("GetIsPC", false);
        engine.SetReturn("GetIsDM", false);
        engine.SetReturn("GetIsDMPossessed", false);
        _scopes.Add(NWScriptRuntime.Override(engine));

        _directory = Path.Combine(Path.GetTempPath(), "mr-p5-7j1-core-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _core = new RecorderProbeCore { Area = AreaOne, Frame = 5 };
        _core.Writer = new CombatRecordWriter(Path.Combine(_directory, "events.jsonl"), Path.Combine(_directory, "summary.json"));
        _core.LastWriter = _core.Writer;
        _core.Writer.Start();
        _core.IsRecording = true;
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            _core.Writer.Complete();
            _core.Writer.Completion.Wait(TimeSpan.FromSeconds(10));
        }
        catch (AggregateException)
        {
            // A writer that failed is not what these tests look at.
        }

        foreach (var scope in _scopes)
            scope.Dispose();
        _scopes.Clear();
        try
        {
            Directory.Delete(_directory, true);
        }
        catch (IOException)
        {
        }
    }

    private static CombatRecordEvent Marker(string tag) =>
        CombatRecordEvent.ResourceChanged(1, CombatRecordResource.Item, -1, -1, CombatRecordSourceKind.Unattributed, "marker", 0, tag);

    private string[] WrittenLines()
    {
        _core.Writer.Complete();
        _core.Writer.Completion.Wait(TimeSpan.FromSeconds(10));
        return File.ReadAllLines(Path.Combine(_directory, "events.jsonl"));
    }

    private static int IndexOf(string[] lines, string tag) => Array.FindIndex(lines, line => line.Contains($"\"{tag}\""));

    [Test]
    public void ThePlatformAndTheRecorderReadTheSameFlag()
    {
        var fresh = new CombatRecordSessionCore();
        ICombatRecordingState state = fresh;

        state.IsRecording.Should().BeFalse();
        fresh.IsRecording = true;
        state.IsRecording.Should().BeTrue();
        ((ICombatRecordSession)fresh).IsRecording.Should().BeTrue();
    }

    [Test]
    public void AnEventOutsideADamageApplicationIsWrittenAtOnceWithTheFrameAndTheClock()
    {
        _core.Frame = 9;
        _core.Millis = 1234;

        _core.Enqueue(Marker("one"));

        _core.EventsEnqueued.Should().Be(1);
        var lines = WrittenLines();
        lines.Should().ContainSingle().Which.Should().Contain("\"ts_ms\":1234").And.Contain("\"frame\":9").And.Contain("\"one\"");
    }

    [Test]
    public void EventsInsideADamageApplicationAreHeldUntilTheOutermostOneEnds()
    {
        var outer = _core.BeginDamageApplication(Alpha, Beta, 10, 100);
        var inner = _core.BeginDamageApplication(Beta, Alpha, 5, 90);
        _core.Enqueue(Marker("first"));
        _core.Enqueue(Marker("second"));

        _core.EventsEnqueued.Should().Be(0);
        _core.EndDamageApplication(inner);
        _core.EventsEnqueued.Should().Be(0, "an inner application leaves the outer one open");
        _core.EndDamageApplication(outer);

        _core.EventsEnqueued.Should().Be(2);
        var lines = WrittenLines();
        IndexOf(lines, "first").Should().BeLessThan(IndexOf(lines, "second"));
        _core.DamageApplications.Should().BeEmpty();
    }

    [Test]
    public void AnEventInsertedAheadOfTheHeldOnesTakesTheApplicationsStartTime()
    {
        _core.Millis = 100;
        var index = _core.BeginDamageApplication(Alpha, Beta, 10, 100);
        _core.Millis = 500;
        _core.Enqueue(Marker("held"));
        _core.Millis = 900;
        _core.HeldInsertIndex = _core.DamageApplications[index].HeldEventStart;
        _core.HeldTimestampMs = _core.DamageApplications[index].StartedMs;
        _core.Enqueue(Marker("ahead"));
        _core.HeldInsertIndex.Should().Be(1, "the next inserted event goes behind it");
        _core.HeldInsertIndex = -1;
        _core.EndDamageApplication(index);

        var lines = WrittenLines();
        IndexOf(lines, "ahead").Should().BeLessThan(IndexOf(lines, "held"));
        lines[IndexOf(lines, "ahead")].Should().Contain("\"ts_ms\":100");
        lines[IndexOf(lines, "held")].Should().Contain("\"ts_ms\":500");
    }

    [Test]
    public void ADamageApplicationNamesTheLastDamageSourceOfARecordedHit()
    {
        _core.BeginDamageApplication(Alpha, Beta, 10, 100);
        _core.BeginDamageApplication(Outsider, Beta, 0, 100);
        _core.BeginDamageApplication(Beta, Beta, 10, 100);

        _core.LastDamageSources.Should().Equal(new Dictionary<uint, uint> { [Beta] = Alpha });
    }

    [Test]
    public void AReactionHittingTheSameCreatureKeepsTheHitPointsTheOuterHitLeft()
    {
        var outer = _core.BeginDamageApplication(Alpha, Beta, 10, 100);
        _core.BeginDamageApplication(Outsider, Beta, 5, 90);

        _core.DamageApplications[outer].HasHitPointsLeftByHit.Should().BeTrue();
        _core.DamageApplications[outer].HitPointsLeftByHit.Should().Be(90);
        _core.FindDamageApplication(Beta).Should().Be(1, "the innermost application on the creature is found first");
        _core.FindDamageApplication(Alpha).Should().Be(-1);
    }

    [Test]
    public void AnUnknownApplicationIndexEndsNothing()
    {
        var index = _core.BeginDamageApplication(Alpha, Beta, 1, 10);

        _core.EndDamageApplication(99);
        _core.EndDamageApplication(-1);

        _core.DamageApplications.Should().HaveCount(1);
        _core.EndDamageApplication(index);
    }

    [Test]
    public void AParticipantIsRegisteredOnceAndItsDeclarationIsWrittenAheadOfTheEventsThatNameIt()
    {
        var first = _core.GetParticipantId(Alpha);
        var again = _core.GetParticipantId(Alpha);
        var second = _core.GetParticipantId(Beta);

        first.Should().Be(1);
        again.Should().Be(1);
        second.Should().Be(2);
        _core.Participants.Should().Equal(Alpha, Beta);
        _core.ParticipantIds.Should().Equal(new Dictionary<uint, int> { [Alpha] = 1, [Beta] = 2 });
        _core.EventsEnqueued.Should().Be(2, "each creature is declared once");
    }

    [Test]
    public void ANonCreatureAndAnInvalidObjectHaveNoParticipantId()
    {
        _core.GetParticipantId(Item).Should().Be(0);
        _core.GetParticipantId(ObjectInvalid).Should().Be(0);
        _core.GetParticipantId(0).Should().Be(0);

        _core.Participants.Should().BeEmpty();
        _core.EventsEnqueued.Should().Be(0);
    }

    [Test]
    public void TheRecordedAreaDecidesWhichEventsAreKept()
    {
        _core.IsInRecordedArea(Alpha).Should().BeTrue();
        _core.IsInRecordedArea(Outsider).Should().BeFalse();
        _core.IsInRecordedArea(ObjectInvalid).Should().BeFalse();
        _core.IsRecorded(Outsider).Should().BeFalse("the other side defaults to no one");
        _core.IsRecorded(Outsider, Alpha).Should().BeTrue();
        _core.AnyInRecordedArea(null!).Should().BeFalse();
        _core.AnyInRecordedArea(new[] { Outsider }).Should().BeFalse();
        _core.AnyInRecordedArea(new[] { Outsider, Beta }).Should().BeTrue();
    }

    [Test]
    public void ReactionsToAHealWaitForItAndAreReleasedOnceItsFrameHasPassed()
    {
        _core.IsInHealingReaction = true;
        _core.AwaitingHealFrame = _core.CurrentFrame();
        _core.AwaitingHealTarget = Beta;
        _core.Enqueue(Marker("reaction"));
        _core.IsInHealingReaction = false;

        _core.AwaitingHealCount.Should().Be(1);
        _core.EventsEnqueued.Should().Be(0);

        _core.Frame += 1;
        _core.Enqueue(Marker("later"));

        _core.AwaitingHealCount.Should().Be(0);
        var lines = WrittenLines();
        IndexOf(lines, "reaction").Should().BeLessThan(IndexOf(lines, "later"));
    }

    [Test]
    public void AHealFrameIsClosedByTheTargetItWasOpenedFor()
    {
        _core.BeginHealApplication(Alpha);
        _core.BeginHealApplication(Beta);
        _core.HealApplications[0] = (Alpha, 30);

        _core.EndHealApplication(Beta).Should().Be(0);
        _core.EndHealApplication(Alpha).Should().Be(30);
        _core.EndHealApplication(Alpha).Should().Be(0, "a frame closes once");
    }

    [Test]
    public void ResetClearsEverythingARecordingKeeps()
    {
        _core.GetParticipantId(Alpha);
        _core.BeginDamageApplication(Alpha, Beta, 10, 100);
        _core.Enqueue(Marker("held"));
        _core.AddPendingDamage(Alpha, Beta, 10, false, default);
        _core.BeginHealApplication(Alpha);
        _core.IsInHealingReaction = true;
        _core.HeldInsertIndex = 0;

        _core.Reset();

        _core.Participants.Should().BeEmpty();
        _core.ParticipantIds.Should().BeEmpty();
        _core.PendingDamage.Should().BeEmpty();
        _core.LastDamageSources.Should().BeEmpty();
        _core.DamageApplications.Should().BeEmpty();
        _core.HealApplications.Should().BeEmpty();
        _core.AwaitingHealCount.Should().Be(0);
        _core.IsInHealingReaction.Should().BeFalse();
        _core.HeldInsertIndex.Should().Be(-1);
        _core.IsRecording.Should().BeTrue("a reset does not stop the recording");
        WrittenLines().Should().NotContain(line => line.Contains("\"held\""), "what was held is dropped by a reset, written by a flush");
    }

    [Test]
    public void FlushEndsTheApplicationsAndWritesWhatWasHeldAndWhatAwaitedItsHeal()
    {
        _core.BeginDamageApplication(Alpha, Beta, 10, 100);
        _core.Enqueue(Marker("held"));
        _core.IsInHealingReaction = true;
        _core.AwaitingHealFrame = _core.CurrentFrame();
        _core.Enqueue(Marker("awaiting"));

        _core.Flush();

        _core.DamageApplications.Should().BeEmpty();
        _core.IsInHealingReaction.Should().BeFalse();
        _core.AwaitingHealCount.Should().Be(0);
        _core.HeldInsertIndex.Should().Be(-1);
        var lines = WrittenLines();
        IndexOf(lines, "held").Should().BeGreaterThanOrEqualTo(0);
        IndexOf(lines, "awaiting").Should().BeGreaterThanOrEqualTo(0);
    }

    [Test]
    public void AFlushAfterTheFlagIsClearedDropsTheHeldEvents()
    {
        _core.BeginDamageApplication(Alpha, Beta, 10, 100);
        _core.Enqueue(Marker("held"));
        _core.IsRecording = false;

        _core.Flush();

        WrittenLines().Should().NotContain(line => line.Contains("\"held\""));
    }

    [Test]
    public void DamageWaitingForTheEngineIsKeptWithTheFrameAndTheClockItWasRegisteredAt()
    {
        _core.Frame = 12;
        _core.Millis = 777;

        _core.AddPendingDamage(Alpha, Beta, 25, true, default);

        var pending = _core.PendingDamage.Should().ContainSingle().Subject;
        pending.Source.Should().Be(Alpha);
        pending.Target.Should().Be(Beta);
        pending.Amount.Should().Be(25);
        pending.IsWeaponHit.Should().BeTrue();
        pending.Frame.Should().Be(12UL);
        pending.TimestampMs.Should().Be(777);
    }

    [Test]
    public void AHitPointsRecordingStaysOnTheApplicationThatTookIt()
    {
        var index = _core.BeginDamageApplication(Alpha, Beta, 10, 100);

        _core.KeepHitPointsLeftByHit(index, 42);

        _core.DamageApplications[index].HasHitPointsLeftByHit.Should().BeTrue();
        _core.DamageApplications[index].HitPointsLeftByHit.Should().Be(42);
    }

    [Test]
    public void TheDefaultSeamsReadTheirOriginalSources()
    {
        var plain = new CombatRecordSessionCore();

        plain.CurrentFrame().Should().Be(0UL);
        plain.UnixTimeMilliseconds().Should().BeGreaterThan(0);
        plain.UtcNow().Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        plain.WriteLog(LogGroup.DM, "default seam");
    }
}
