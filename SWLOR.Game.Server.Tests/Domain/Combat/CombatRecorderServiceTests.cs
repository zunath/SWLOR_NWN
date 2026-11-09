using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The combat recorder instance. The engine is a recording fake with a few creatures in two areas, the calls into code
/// that is not converted yet (abilities, stats, hooks) are answered by a subclass, and the real writer writes its files
/// into a folder of the test's own. The scope structs dispose through the static facade, so the fixture installs a
/// container that holds the instance under test.
/// </summary>
[TestFixture]
public class CombatRecorderServiceTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint AreaOne = 100;
    private const uint AreaTwo = 200;
    private const uint Alpha = 1;
    private const uint Beta = 2;
    private const uint Gamma = 3;
    private const uint Outsider = 4;
    private const uint Item = 50;

    private readonly List<IDisposable> _scopes = new();
    private readonly Dictionary<uint, uint> _areaOf = new();
    private readonly HashSet<uint> _creatures = new();
    private readonly HashSet<uint> _pcs = new();
    private readonly HashSet<uint> _gone = new();
    private readonly List<uint> _online = new();
    private string _directory = null!;
    private FakeNWScript _engine = null!;
    private Probe _recorder = null!;
    private FacadeContainer _facades = null!;
    private uint _killer;
    private uint _self;
    private uint _lastPlayerDied;
    private uint _lastHostile;
    private int _cursor;

    private static AbilityDetail Strike => new() { Name = "Strike", AbilityLevel = 3, ActivationType = AbilityActivationType.Casted };

    [SetUp]
    public void SetUp()
    {
        _areaOf.Clear();
        _creatures.Clear();
        _pcs.Clear();
        _gone.Clear();
        _online.Clear();
        foreach (var creature in new[] { Alpha, Beta, Gamma })
        {
            _areaOf[creature] = AreaOne;
            _creatures.Add(creature);
        }

        _areaOf[Outsider] = AreaTwo;
        _creatures.Add(Outsider);
        _areaOf[AreaOne] = AreaOne;
        _areaOf[AreaTwo] = AreaTwo;
        _areaOf[Item] = ObjectInvalid;
        _pcs.Add(Alpha);
        _online.Add(Alpha);
        _killer = _self = _lastPlayerDied = _lastHostile = ObjectInvalid;

        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsObjectValid", args =>
            (uint)args[0]! != ObjectInvalid && (uint)args[0]! != 0 && !_gone.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetArea", args => _areaOf.GetValueOrDefault((uint)args[0]!, ObjectInvalid));
        _engine.SetReturnHandler("GetObjectType", args => _creatures.Contains((uint)args[0]!) ? ObjectType.Creature : ObjectType.Item);
        _engine.SetReturnHandler("GetName", args => $"name-{(uint)args[0]!}");
        _engine.SetReturnHandler("GetResRef", args => $"res-{(uint)args[0]!}");
        _engine.SetReturnHandler("GetIsPC", args => _pcs.Contains((uint)args[0]!));
        _engine.SetReturn("GetIsDM", false);
        _engine.SetReturn("GetIsDMPossessed", false);
        _engine.SetReturnHandler("GetObjectUUID", args => $"uuid-{(uint)args[0]!}");
        _engine.SetReturnHandler("GetCurrentHitPoints", _ => 80);
        _engine.SetReturnHandler("GetMaxHitPoints", _ => 100);
        _engine.SetReturn("GetIsInCombat", true);
        _engine.SetReturn("GetIsDead", false);
        _engine.SetReturnHandler("GetFirstPC", _ =>
        {
            _cursor = 0;
            return _online.Count > 0 ? _online[0] : ObjectInvalid;
        });
        _engine.SetReturnHandler("GetNextPC", _ =>
        {
            _cursor++;
            return _cursor < _online.Count ? _online[_cursor] : ObjectInvalid;
        });
        _engine.SetReturnHandler("GetItemPossessor", _ => Alpha);
        _engine.SetReturnHandler("OBJECT_SELF", _ => _self);
        _engine.SetReturnHandler("GetLastKiller", _ => _killer);
        _engine.SetReturnHandler("GetLastPlayerDied", _ => _lastPlayerDied);
        _engine.SetReturnHandler("GetLastHostileActor", _ => _lastHostile);
        _scopes.Add(NWScriptRuntime.Override(_engine));

        _directory = Path.Combine(Path.GetTempPath(), "mr-p2-d6-" + Guid.NewGuid().ToString("N"));
        var db = new InMemoryDBService();
        foreach (var player in new[] { Alpha })
            db.With(new Player { Id = $"uuid-{player}" });
        var presence = ScriptedProxy.Create<IDMPresenceService>(out var presenceScript);
        presenceScript.On("IsControlledByDM", _ => false);
        _recorder = new Probe(presence, db, _directory + "/");

        _facades = new FacadeContainer(services =>
            services.Replace(ServiceDescriptor.Singleton<ICombatRecorderService>(_recorder)));
        _scopes.Add(_facades.Install());
    }

    [TearDown]
    public void TearDown()
    {
        if (_recorder.IsRecording)
            _recorder.Stop("teardown", out _);
        WaitForWriter();
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

    private void WaitForWriter()
    {
        try
        {
            _recorder.LastWriter?.Completion.Wait(TimeSpan.FromSeconds(10));
        }
        catch (AggregateException)
        {
            // A writer that failed is the point of the test that made it.
        }
    }

    private void Start(string label = "unit")
    {
        _recorder.Start(AreaOne, label, out var message).Should().BeTrue(message);
    }

    /// <summary>Stops the recording, waits for the writer and returns the events it wrote.</summary>
    private List<JsonElement> StopAndRead()
    {
        if (_recorder.IsRecording)
            _recorder.Stop("test", out _);
        var writer = _recorder.LastWriter!;
        writer.Completion.Wait(TimeSpan.FromSeconds(10)).Should().BeTrue("the writer finishes its files");
        return File.ReadAllLines(writer.EventsPath)
            .Select(line => JsonDocument.Parse(line).RootElement.Clone())
            .ToList();
    }

    private static List<JsonElement> Of(List<JsonElement> events, string type) =>
        events.Where(e => e.GetProperty("type").GetString() == type).ToList();

    [Test]
    public void Start_NeedsAValidArea()
    {
        _recorder.Start(ObjectInvalid, "x", out var message).Should().BeFalse();
        message.Should().Contain("valid area");
        _recorder.Start(Alpha, "x", out message).Should().BeFalse("a creature is not an area");
        _recorder.IsRecording.Should().BeFalse();
        _recorder.RecordedArea.Should().Be(ObjectInvalid);
    }

    [Test]
    public void Start_BeginsARecordingThatRefusesASecondOne()
    {
        _recorder.Start(AreaOne, "  fight  ", out var message).Should().BeTrue();

        message.Should().Contain("'fight'").And.Contain("name-100");
        _recorder.IsRecording.Should().BeTrue();
        _recorder.RecordedArea.Should().Be(AreaOne);
        _recorder.Hooks.Should().Be(1);
        _recorder.Scheduled.Should().NotBeNull();
        _recorder.Start(AreaOne, "again", out message).Should().BeFalse();
        message.Should().Contain("already running");
        _recorder.WriteLines.Should().Contain(line => line.Contains("started in"));
    }

    [Test]
    public void Start_DefaultsTheLabelToTheAreaResref()
    {
        _recorder.Start(AreaOne, "  ", out var message).Should().BeTrue();

        message.Should().Contain("'res-100'");
    }

    [Test]
    public void Start_WarnsWhenTheDamageHooksCannotBeInstalled()
    {
        _recorder.HookFailure = new InvalidOperationException("no native layer");

        _recorder.Start(AreaOne, "x", out var message).Should().BeTrue();

        message.Should().Contain("WARNING").And.Contain("no native layer");
        _recorder.GetStatus().Should().Contain("WARNING");
        _recorder.WriteLines.Should().Contain(line => line.Contains("could not hook"));
    }

    [Test]
    public void Stop_ReportsWhenNothingRuns()
    {
        _recorder.Stop("none", out var message).Should().BeFalse();

        message.Should().Contain("No combat recording");
    }

    [Test]
    public void Stop_WritesTheFilesAndClearsTheState()
    {
        Start("run");
        _recorder.Frame = 5;

        _recorder.Stop("done", out var message).Should().BeTrue();

        message.Should().Contain("stopped after").And.Contain(".summary.json");
        _recorder.IsRecording.Should().BeFalse();
        _recorder.Scheduled!.Disposed.Should().BeTrue();
        var events = StopAndRead();
        events.First().GetProperty("type").GetString().Should().Be("recording_started");
        events.First().GetProperty("label").GetString().Should().Be("run");
        events.Last().GetProperty("type").GetString().Should().Be("recording_stopped");
        events.Last().GetProperty("reason").GetString().Should().Be("done");
        _recorder.LastWriter!.SummaryPath.Should().Be(_recorder.LastWriter.SummaryPath);
        File.Exists(_recorder.LastWriter.SummaryPath).Should().BeTrue();
    }

    [Test]
    public void GetStatus_DescribesNoRecordingTheRunningOneAndTheFinishedOne()
    {
        _recorder.GetStatus().Should().Be("No combat recording is running.");

        Start("status");
        _recorder.GetStatus().Should().Contain("Recording 'status'").And.Contain("participants");

        _recorder.Stop("x", out _);
        _recorder.LastWriter!.Completion.Wait(TimeSpan.FromSeconds(10));
        _recorder.GetStatus().Should().Contain("(finished)");
    }

    [Test]
    public void GetStatus_ReportsAFailedWriter()
    {
        Directory.CreateDirectory(_directory);
        var blocker = _directory + "/CombatRecordings";
        File.WriteAllText(blocker, "a file where the recordings folder should be");

        _recorder.Start(AreaOne, "broken", out _).Should().BeTrue();
        SpinWait.SpinUntil(() => _recorder.LastWriter!.Error != null, TimeSpan.FromSeconds(10)).Should().BeTrue();

        _recorder.GetStatus().Should().Contain("WRITER ERROR");
        _recorder.Stop("x", out _);
        WaitForWriter();
        _recorder.GetStatus().Should().Contain("failed:");
    }

    [Test]
    public void StopOnShutdown_StopsTheRecordingAndWaitsForTheWriters()
    {
        Start();

        _recorder.StopOnShutdown();

        _recorder.IsRecording.Should().BeFalse();
        _recorder.LastWriter!.Completion.IsCompleted.Should().BeTrue();
        _recorder.StopOnShutdown();
    }

    [Test]
    public void Start_RegistersPlayersAndSnapshotsEveryParticipant()
    {
        Start();

        var events = StopAndRead();

        var participants = Of(events, "participant");
        participants.Should().ContainSingle();
        participants[0].GetProperty("name").GetString().Should().Be("name-1");
        participants[0].GetProperty("player").GetBoolean().Should().BeTrue();
        participants[0].TryGetProperty("resref", out _).Should().BeFalse("a player's resref is not recorded");
        var snapshot = Of(events, "snapshot").First();
        snapshot.GetProperty("hp").GetInt32().Should().Be(80 - 7);
        snapshot.GetProperty("temp_hp").GetInt32().Should().Be(7);
        snapshot.GetProperty("fp").GetInt32().Should().Be(11);
        snapshot.GetProperty("max_stm").GetInt32().Should().Be(44);
    }

    [Test]
    public void Snapshots_StopTheRecordingWhenTheAreaIsGoneAndSkipPlayersWithoutASave()
    {
        Start();
        _recorder.Scheduled!.Action();
        _recorder.IsRecording.Should().BeTrue();

        _gone.Add(AreaOne);
        _recorder.Scheduled!.Action();

        _recorder.IsRecording.Should().BeFalse();
        _recorder.Stop("again", out _).Should().BeFalse();
    }

    [Test]
    public void Snapshots_SkipAPlayerWhoseSaveIsMissing()
    {
        _pcs.Add(Beta);
        _online.Add(Beta);
        Start();

        var snapshots = Of(StopAndRead(), "snapshot");

        snapshots.Select(e => e.GetProperty("creature").GetInt32()).Should().OnlyContain(id => id == 1,
            "the second player has no saved character");
    }

    [Test]
    public void Snapshots_LeaveOutPlayersControlledByADM()
    {
        var presence = ScriptedProxy.Create<IDMPresenceService>(out var script);
        script.On("IsControlledByDM", _ => true);
        var recorder = new Probe(presence, new InMemoryDBService(), _directory + "/");
        recorder.Start(AreaOne, "dm", out _).Should().BeTrue();

        recorder.Stop("x", out _);
        recorder.LastWriter!.Completion.Wait(TimeSpan.FromSeconds(10));

        File.ReadAllText(recorder.LastWriter.EventsPath).Should().NotContain("\"participant\"");
    }

    [Test]
    public void BeginAttribution_DoesNothingWhileNoRecordingRuns()
    {
        var scope = _recorder.BeginAttribution(Alpha, CombatRecordSourceKind.Item);
        _recorder.BeginAbilityAttribution(Alpha, Strike).Should().Be(default(CombatRecordAttributionScope));
        _recorder.BeginStatusEffectAttribution(Alpha, Beta, Status()).Should().Be(default(CombatRecordAttributionScope));
        _recorder.BeginItemAttribution(Alpha, Item).Should().Be(default(CombatRecordAttributionScope));
        _recorder.BeginStatTriggerAttribution(Alpha, StatType.AttackPercentAdjustment).Should().Be(default(CombatRecordAttributionScope));
        _recorder.BeginRiderAttribution(Alpha, CombatRecordSourceKind.Lifesteal).Should().Be(default(CombatRecordAttributionScope));
        _recorder.BeginAbilityFeatAttribution(Alpha, FeatType.Invalid).Should().Be(default(CombatRecordAttributionScope));
        _recorder.BeginTrapAttribution(Alpha, default).Should().Be(default(CombatRecordAttributionScope));
        _recorder.BeginSetupAttribution(Alpha).Should().Be(default(CombatRecordAttributionScope));
        _recorder.BeginRespawnAttribution().Should().Be(default(CombatRecordAttributionScope));
        _recorder.BeginHealingReaction(Alpha).Should().Be(default(CombatRecordHealingReactionScope));
        _recorder.BeginAbilityCost(Alpha, Strike).Should().Be(default(CombatRecordAbilityCostScope));
        _recorder.CaptureScope().Actor.Should().Be(0);
        _recorder.CaptureAttackAttribution(Alpha).Actor.Should().Be(0);
        _recorder.BeginAttribution(default(CombatRecordAttribution)).Should().Be(default(CombatRecordAttributionScope));
        scope.Dispose();
    }

    private IStatusEffect Status(string name = "Burn", uint source = Alpha)
    {
        var status = ScriptedProxy.Create<IStatusEffect>(out var script);
        script.On("get_Name", _ => name);
        script.On("get_Source", _ => source);
        script.On("get_OriginatingAbility", _ => null);
        script.On("get_WasNaturallyExpired", _ => false);
        return status;
    }

    [Test]
    public void Scopes_NestAndRestoreTheirDepthWhenDisposed()
    {
        Start();

        var outer = _recorder.BeginAttribution(Alpha, CombatRecordSourceKind.Item, "Medpac", 5, 2);
        var inner = _recorder.BeginSetupAttribution(Alpha);
        _recorder.CaptureScope().Kind.Should().Be(CombatRecordSourceKind.Setup);
        inner.Dispose();
        var captured = _recorder.CaptureScope();
        captured.Kind.Should().Be(CombatRecordSourceKind.Item);
        captured.Name.Should().Be("Medpac");
        captured.Recording.Should().Be(1);
        outer.Dispose();

        _recorder.CaptureScope().Actor.Should().Be(0, "no scope is open any more");
        _recorder.RestoreAttributionDepth(-1);
        _recorder.RestoreAttributionDepth(10);
    }

    [Test]
    public void Scopes_StopGrowingAtTheMaximumDepth()
    {
        Start();
        var scopes = Enumerable.Range(0, 40).Select(_ => _recorder.BeginAttribution(Alpha, CombatRecordSourceKind.Item)).ToList();

        scopes.Reverse();
        foreach (var scope in scopes)
            scope.Dispose();

        _recorder.CaptureScope().Actor.Should().Be(0);
    }

    [Test]
    public void BeginAbilityAttribution_NamesTheFeatFromTheRegisteredAbility()
    {
        var ability = Strike;
        _recorder.Abilities[FeatType.Alertness] = ability;
        Start();

        using (_recorder.BeginAbilityAttribution(Alpha, ability))
        {
            var scope = _recorder.CaptureScope();
            scope.Kind.Should().Be(CombatRecordSourceKind.Ability);
            scope.Name.Should().Be("Strike");
            scope.Code.Should().Be((int)FeatType.Alertness);
            scope.Rank.Should().Be(3);
        }

        using (_recorder.BeginAbilityAttribution(Alpha, ability, FeatType.Invalid))
            _recorder.CaptureScope().Code.Should().Be((int)FeatType.Alertness);
        using (_recorder.BeginAbilityAttribution(Alpha, ability, FeatType.Ambidexterity))
            _recorder.CaptureScope().Code.Should().Be((int)FeatType.Ambidexterity);
        using (_recorder.BeginAbilityAttribution(Alpha, new AbilityDetail { Name = "Unregistered" }))
            _recorder.CaptureScope().Code.Should().Be((int)FeatType.Invalid);
        _recorder.BeginAbilityAttribution(Alpha, null).Should().Be(default(CombatRecordAttributionScope));
    }

    [Test]
    public void BeginStatusEffectAttribution_FallsBackToTheAffectedCreatureWhenTheSourceIsGone()
    {
        Start();

        using (_recorder.BeginStatusEffectAttribution(ObjectInvalid, Beta, Status("Poison")))
        {
            var scope = _recorder.CaptureScope();
            scope.Actor.Should().Be(Beta);
            scope.Owner.Should().Be(Beta);
            scope.Name.Should().Be("Poison");
        }

        using (_recorder.BeginStatusEffectAttribution(Alpha, Beta, Status()))
            _recorder.CaptureScope().Actor.Should().Be(Alpha);
    }

    [Test]
    public void BeginItemAndStatTriggerAttributions_NameTheirSource()
    {
        Start();

        using (_recorder.BeginItemAttribution(Alpha, Item))
            _recorder.CaptureScope().Name.Should().Be("name-50");
        using (_recorder.BeginStatTriggerAttribution(Alpha, StatType.AttackPercentAdjustment))
        {
            _recorder.CaptureScope().Kind.Should().Be(CombatRecordSourceKind.StatTrigger);
            _recorder.CaptureScope().Code.Should().Be((int)StatType.AttackPercentAdjustment);
        }
    }

    [Test]
    public void BeginAbilityFeatAttribution_NeedsARegisteredFeat()
    {
        _recorder.Abilities[FeatType.Alertness] = Strike;
        Start();

        _recorder.BeginAbilityFeatAttribution(Alpha, FeatType.Ambidexterity).Should().Be(default(CombatRecordAttributionScope));
        using (_recorder.BeginAbilityFeatAttribution(Alpha, FeatType.Alertness))
        {
            _recorder.CaptureScope().Name.Should().Be("Strike");
            _recorder.CaptureScope().Code.Should().Be((int)FeatType.Alertness);
        }
    }

    [Test]
    public void BeginRiderAttribution_KeepsTheEnclosingAbilitysName()
    {
        _recorder.Abilities[FeatType.Alertness] = Strike;
        Start();

        using (_recorder.BeginAbilityFeatAttribution(Alpha, FeatType.Alertness))
        using (_recorder.BeginRiderAttribution(Alpha, CombatRecordSourceKind.Lifesteal))
        {
            var rider = _recorder.CaptureScope();
            rider.Kind.Should().Be(CombatRecordSourceKind.Lifesteal);
            rider.Name.Should().Be("Strike");
            rider.Rank.Should().Be(3);
        }

        using (_recorder.BeginRiderAttribution(Alpha, CombatRecordSourceKind.DamageShare))
            _recorder.CaptureScope().Name.Should().BeNull("there is no enclosing ability");
    }

    [Test]
    public void CapturedScopes_AreRestoredOnlyInTheSameRecording()
    {
        Start();
        CombatRecordAttribution captured;
        using (_recorder.BeginAttribution(Alpha, CombatRecordSourceKind.Item, "Medpac"))
            captured = _recorder.CaptureScope();

        using (_recorder.BeginAttribution(captured))
            _recorder.CaptureScope().Name.Should().Be("Medpac");
        using (_recorder.BeginTrapAttribution(Beta, captured))
            _recorder.CaptureScope().Name.Should().Be("Medpac", "the same recording restores the placing scope");

        _recorder.Stop("one", out _);
        Start();

        _recorder.BeginAttribution(captured).Should().Be(default(CombatRecordAttributionScope));
        using (_recorder.BeginTrapAttribution(Beta, captured))
        {
            _recorder.CaptureScope().Kind.Should().Be(CombatRecordSourceKind.Trap);
            _recorder.CaptureScope().Actor.Should().Be(Beta);
        }
    }

    [Test]
    public void CaptureAttackAttribution_UsesTheAttackersOwnScopeOrASwing()
    {
        Start();

        var swing = _recorder.CaptureAttackAttribution(Alpha);
        swing.Kind.Should().Be(CombatRecordSourceKind.Autoattack);
        swing.Code.Should().Be((int)SkillType.Lightsaber);
        swing.Recording.Should().Be(1);

        using (_recorder.BeginAttribution(Alpha, CombatRecordSourceKind.Ability, "Strike"))
        {
            _recorder.CaptureAttackAttribution(Alpha).Name.Should().Be("Strike");
            _recorder.CaptureAttackAttribution(Beta).Kind.Should().Be(CombatRecordSourceKind.Autoattack, "another creature's scope is not lent");
        }
    }

    [Test]
    public void RecordAbilityCast_WritesTheCasterTargetAndLocation()
    {
        Start();
        _engine.SetReturnHandler("GetAreaFromLocation", _ => AreaOne);
        _engine.SetReturnHandler("GetPositionFromLocation", _ => new System.Numerics.Vector3(1.234f, 2f, 3f));
        var location = EngineStructures.Create<Location>(5);

        _recorder.RecordAbilityCast(Alpha, ObjectInvalid, location, Strike, FeatType.Alertness, 1.5f, true);
        _recorder.RecordAbilityCast(Alpha, Beta, location, null!, FeatType.Alertness, 1.5f);
        _recorder.RecordAbilityCast(Alpha, Beta, location, Strike, FeatType.Alertness, 0.5f);

        var casts = Of(StopAndRead(), "ability_cast");
        casts.Should().HaveCount(2);
        casts[0].GetProperty("location").GetProperty("x").GetDouble().Should().Be(1.23);
        casts[0].GetProperty("toggled_off").GetBoolean().Should().BeTrue();
        casts[0].GetProperty("delay_s").GetDouble().Should().Be(1.5);
        casts[1].TryGetProperty("location", out _).Should().BeFalse("a valid target needs no location");
        casts[1].GetProperty("target").GetInt32().Should().BeGreaterThan(0);
    }

    [Test]
    public void AbilityDeniedQueuedAndInterruptedEventsAreWrittenOnlyForTheRecordedArea()
    {
        Start();

        _recorder.RecordAbilityDenied(Alpha, Beta, Strike, FeatType.Alertness, "no FP", true);
        _recorder.RecordAbilityDenied(Outsider, Outsider, Strike, FeatType.Alertness, "elsewhere", false);
        _recorder.RecordAbilityQueued(Alpha, Beta, Strike, FeatType.Alertness);
        _recorder.RecordAbilityQueued(Alpha, Beta, null!, FeatType.Alertness);
        _recorder.RecordAbilityInterrupted(Alpha, Strike, CombatRecordInterruptCause.Moved);
        _recorder.RecordAbilityInterrupted(Outsider, Strike, CombatRecordInterruptCause.Moved);

        var events = StopAndRead();
        var denied = Of(events, "ability_denied").Single();
        denied.GetProperty("reason").GetString().Should().Be("no FP");
        denied.GetProperty("stage").GetString().Should().Be("completion");
        Of(events, "ability_queued").Should().ContainSingle();
        Of(events, "ability_interrupted").Should().ContainSingle()
            .Which.GetProperty("cause").GetString().Should().Be("moved");
    }

    [Test]
    public void RecordAbilityInterrupted_NamesTheCreatureThatCausedIt()
    {
        Start();

        _recorder.RecordAbilityInterrupted(Alpha, Strike, CombatRecordInterruptCause.Damage, Beta);
        _recorder.RecordAbilityInterrupted(Alpha, Strike, CombatRecordInterruptCause.Damage, Alpha);
        using (_recorder.BeginAttribution(Gamma, CombatRecordSourceKind.Ability, "Stun"))
            _recorder.RecordAbilityInterrupted(Alpha, Strike, CombatRecordInterruptCause.Other);
        _recorder.RecordAbilityInterrupted(Alpha, Strike, CombatRecordInterruptCause.Other);

        var interrupted = Of(StopAndRead(), "ability_interrupted");
        interrupted.Should().HaveCount(4);
        interrupted[0].GetProperty("interrupted_by_kind").GetString().Should().Be("unattributed");
        interrupted[1].TryGetProperty("interrupted_by_kind", out _).Should().BeFalse("self damage names no one");
        interrupted[2].GetProperty("cause").GetString().Should().Be("crowd_control");
        interrupted[2].GetProperty("interrupted_by_name").GetString().Should().Be("Stun");
        interrupted[3].GetProperty("cause").GetString().Should().Be("other");
    }

    [Test]
    public void ImpactsAreWrittenForTheRecordedAreaAndForPulsesThatLandInIt()
    {
        Start();
        var summary = new AbilityImpactSummary { ImpactedTargetCount = 2, CriticalHitCount = 1, AttributedDamage = 40, ImpactedTargets = new[] { Beta } };
        var outside = new AbilityImpactSummary { ImpactedTargets = new[] { Outsider } };

        _recorder.RecordAbilityImpact(Alpha, Beta, default!, Strike, FeatType.Alertness, summary);
        _recorder.RecordAbilityImpact(Outsider, Outsider, default!, Strike, FeatType.Alertness, summary);
        _recorder.RecordAbilityImpact(Outsider, Outsider, default!, Strike, FeatType.Alertness, outside);
        _recorder.RecordAbilityImpact(Outsider, Outsider, default!, Strike, FeatType.Alertness, null!);
        _recorder.RecordAbilityImpact(Alpha, Beta, default!, null!, FeatType.Alertness, summary);
        _recorder.RecordRecurringAbilityImpact(Outsider, Strike, summary);
        _recorder.RecordRecurringAbilityImpact(Outsider, Strike, null!);
        _recorder.RecordRecurringAbilityImpact(Alpha, null!, summary);
        _recorder.RecordRecurringAbilityImpact(Alpha, Strike, null!);

        var impacts = Of(StopAndRead(), "ability_impact");
        impacts.Should().HaveCount(4);
        impacts[0].GetProperty("targets_hit").GetInt32().Should().Be(2);
        impacts[0].GetProperty("damage_queued").GetInt32().Should().Be(40);
        impacts[0].GetProperty("recurring").GetBoolean().Should().BeFalse();
        impacts[2].GetProperty("recurring").GetBoolean().Should().BeTrue();
        impacts[3].GetProperty("targets_hit").GetInt32().Should().Be(0);
    }

    [Test]
    public void ToggleDeactivationIsTakenOnceInTheFrameItWasNoted()
    {
        _recorder.Frame = 4;
        _recorder.NoteToggleDeactivated(Alpha);

        _recorder.TakeToggleDeactivation(Beta).Should().BeFalse();
        _recorder.NoteToggleDeactivated(Alpha);
        _recorder.Frame = 5;
        _recorder.TakeToggleDeactivation(Alpha).Should().BeFalse("it was noted in an earlier frame");
        _recorder.NoteToggleDeactivated(Alpha);
        _recorder.TakeToggleDeactivation(Alpha).Should().BeTrue();
        _recorder.TakeToggleDeactivation(Alpha).Should().BeFalse("taking it clears it");
    }

    [Test]
    public void AbilityCostScope_RecordsTheFpAndStaminaTheActivationTook()
    {
        Start();
        _recorder.Fp[Alpha] = 50;
        _recorder.Stamina[Alpha] = 30;

        var cost = _recorder.BeginAbilityCost(Alpha, Strike, FeatType.Alertness);
        _recorder.CaptureScope().Kind.Should().Be(CombatRecordSourceKind.AbilityCost);
        _recorder.Fp[Alpha] = 40;
        _recorder.Stamina[Alpha] = 25;
        cost.Dispose();

        _recorder.BeginAbilityCost(Outsider, Strike).Should().Be(default(CombatRecordAbilityCostScope));
        _recorder.BeginAbilityCost(Alpha, null!).Should().Be(default(CombatRecordAbilityCostScope));
        var recorded = Of(StopAndRead(), "ability_cost").Single();
        recorded.GetProperty("fp").GetInt32().Should().Be(10);
        recorded.GetProperty("stm").GetInt32().Should().Be(5);
        recorded.GetProperty("ability").GetString().Should().Be("Strike");
    }

    [Test]
    public void AbilityCostScope_WritesNothingWhenTheRecordingEndedOrTheActivatorIsGone()
    {
        Start();
        var first = _recorder.BeginAbilityCost(Alpha, Strike);
        _areaOf.Remove(Alpha);
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != ObjectInvalid && (uint)args[0]! != Alpha);
        first.Dispose();
        _areaOf[Alpha] = AreaOne;
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != ObjectInvalid && (uint)args[0]! != 0);
        var second = _recorder.BeginAbilityCost(Alpha, Strike);
        _recorder.Stop("x", out _);
        second.Dispose();

        Of(StopAndRead(), "ability_cost").Should().BeEmpty();
    }

    [TestCase(1, "hit")]
    [TestCase(7, "hit")]
    [TestCase(2, "deflect")]
    [TestCase(3, "critical")]
    [TestCase(4, "miss")]
    [TestCase(99, "unknown")]
    public void RecordAttack_NamesTheNativeResult(int result, string expected)
    {
        Start();

        _recorder.RecordAttack(Alpha, Beta, result, 75, "Slash");
        _recorder.RecordAttack(Outsider, Outsider, result, 75, "Elsewhere");

        var attack = Of(StopAndRead(), "attack").Single();
        attack.GetProperty("result").GetString().Should().Be(expected);
        attack.GetProperty("hit_chance").GetInt32().Should().Be(75);
        attack.GetProperty("ability").GetString().Should().Be("Slash");
    }

    private static CombatRecordDamageBreakdown Damage(int physical = 0, int fire = 0)
    {
        var damage = new CombatRecordDamageBreakdown();
        damage.AddNative(0, physical);
        damage.AddNative(8, fire);
        return damage;
    }

    private void ApplyDamage(uint source, uint target, int amount, int before = 100, int after = 90, bool engine = true,
        CombatRecordTemporaryHitPointPool[]? poolsBefore = null, CombatRecordTemporaryHitPointPool[]? poolsAfter = null)
    {
        var index = _recorder.BeginDamageApplication(source, target, amount, before);
        _recorder.RecordDamageApplied(index, engine, source, target, Damage(physical: amount), before, after, 100,
            poolsBefore!, poolsAfter!);
    }

    [Test]
    public void DamageInsideAnAbilityScope_IsAttributedToIt()
    {
        Start();

        using (_recorder.BeginAttribution(Alpha, CombatRecordSourceKind.Ability, "Strike", 9, 3))
            ApplyDamage(Alpha, Beta, 10);

        var damage = Of(StopAndRead(), "damage").Single();
        damage.GetProperty("source_kind").GetString().Should().Be("ability");
        damage.GetProperty("source_name").GetString().Should().Be("Strike");
        damage.GetProperty("total").GetInt32().Should().Be(10);
        damage.GetProperty("hp_lost").GetInt32().Should().Be(10);
        damage.GetProperty("damage").GetProperty("physical").GetInt32().Should().Be(10);
    }

    [Test]
    public void WeaponDamageRegisteredBeforeTheAnimation_IsMatchedByAmount()
    {
        Start();
        _recorder.Millis = 1000;

        _recorder.ExpectWeaponDamage(Alpha, Beta, 10, SkillType.HeavyVibroblade);
        _recorder.ExpectWeaponDamage(Alpha, Beta, 0, SkillType.HeavyVibroblade);
        _recorder.HasPendingDamage(Alpha, Beta).Should().BeTrue();
        _recorder.HasPendingDamage(Beta, Alpha).Should().BeFalse();
        ApplyDamage(Alpha, Beta, 10);

        _recorder.HasPendingDamage(Alpha, Beta).Should().BeFalse("the registration was consumed");
        var damage = Of(StopAndRead(), "damage").Single();
        damage.GetProperty("source_kind").GetString().Should().Be("autoattack");
        damage.GetProperty("source_name").GetString().Should().Be("HeavyVibroblade");
    }

    [Test]
    public void DamageWithoutAScopeOrRegistration_FallsBackToTheCreatorsActiveAbility()
    {
        Start();
        _recorder.ActiveImpacts[Alpha] = new AbilityImpactSummary { Ability = Strike };

        ApplyDamage(Alpha, Beta, 10);
        ApplyDamage(Gamma, Beta, 10);

        var damage = Of(StopAndRead(), "damage");
        damage[0].GetProperty("source_name").GetString().Should().Be("Strike");
        damage[1].GetProperty("source_kind").GetString().Should().Be("unattributed");
    }

    [Test]
    public void ExpectDeferredDamage_RegistersStatusTicksAndTheirDamageIsAttributed()
    {
        Start();
        IStatusEffect burn = Status("Burn");

        using (_recorder.BeginStatusEffectAttribution(Alpha, Beta, burn))
            _recorder.ExpectDeferredDamage(Alpha, Beta, 5, CombatDamageDeliveryType.Direct);
        _recorder.HasPendingDamage(Alpha, Beta).Should().BeTrue();
        _recorder.Frame = 1;
        ApplyDamage(Alpha, Beta, 5, engine: false);

        var damage = Of(StopAndRead(), "damage").Single();
        damage.GetProperty("source_kind").GetString().Should().Be("status_effect");
        damage.GetProperty("source_name").GetString().Should().Be("Burn");
    }

    [Test]
    public void ExpectDeferredDamage_IgnoresWhatTheImpactAlreadyCovers()
    {
        Start();
        _recorder.ActiveImpacts[Alpha] = new AbilityImpactSummary { Ability = Strike };

        _recorder.ExpectDeferredDamage(Alpha, Beta, 0, CombatDamageDeliveryType.DamageOverTime);
        _recorder.ExpectDeferredDamage(Outsider, Outsider, 5, CombatDamageDeliveryType.DamageOverTime);
        _recorder.ExpectDeferredDamage(Alpha, Beta, 5, CombatDamageDeliveryType.Direct);
        _recorder.ExpectDeferredDamage(Alpha, Beta, 5, CombatDamageDeliveryType.Triggered);
        _recorder.ExpectDeferredDamage(Alpha, Beta, 5, CombatDamageDeliveryType.DamageOverTime);
        using (_recorder.BeginAttribution(Alpha, CombatRecordSourceKind.Ability, "Strike"))
            _recorder.ExpectDeferredDamage(Alpha, Beta, 5, CombatDamageDeliveryType.Direct);

        _recorder.HasPendingDamage(Alpha, Beta).Should().BeFalse();
    }

    [Test]
    public void ExpectDeferredDamage_RegistersASourceScopeWithNoImpactAndMarksTransfersAsShares()
    {
        Start();

        using (_recorder.BeginAttribution(Alpha, CombatRecordSourceKind.Trap))
            _recorder.ExpectDeferredDamage(Alpha, Beta, 5, CombatDamageDeliveryType.Direct);
        _recorder.ExpectDeferredDamage(ObjectInvalid, Beta, 7, CombatDamageDeliveryType.Transferred);

        ApplyDamage(Alpha, Beta, 5, engine: false);
        ApplyDamage(Beta, Beta, 7, engine: false);
        var damage = Of(StopAndRead(), "damage");
        damage[0].GetProperty("source_kind").GetString().Should().Be("trap");
        damage[1].GetProperty("source_kind").GetString().Should().Be("damage_share");
    }

    [Test]
    public void ExpectHitPointCost_IsRecordedAsDamageToTheCaster()
    {
        Start();
        _recorder.ExpectHitPointCost(Alpha, 0);
        _recorder.ExpectHitPointCost(Outsider, 5);
        using (_recorder.BeginAbilityAttribution(Alpha, Strike))
            _recorder.ExpectHitPointCost(Alpha, 5);
        _recorder.HasPendingDamage(Alpha, Alpha).Should().BeTrue();
        ApplyDamage(Alpha, Alpha, 5);

        Of(StopAndRead(), "damage").Single().GetProperty("source_kind").GetString().Should().Be("ability_cost");
    }

    [Test]
    public void PendingDamageThatNeverLands_ExpiresAfterItsWindow()
    {
        Start();
        _recorder.Millis = 1000;
        _recorder.Frame = 1;
        _recorder.ExpectDeferredDamage(Alpha, Beta, 5, CombatDamageDeliveryType.Transferred);
        _recorder.Frame = 500;
        _recorder.Millis = 999999;

        ApplyDamage(Alpha, Beta, 5, engine: false);

        Of(StopAndRead(), "damage").Single().GetProperty("source_kind").GetString().Should().Be("unattributed");
    }

    [Test]
    public void DamageThatChangesNothing_IsNotRecorded()
    {
        Start();

        ApplyDamage(Alpha, Beta, 0, before: 100, after: 100);
        ApplyDamage(Outsider, Outsider, 10);

        Of(StopAndRead(), "damage").Should().BeEmpty();
    }

    [Test]
    public void DamageApplicationsOutOfRangeOrAfterTheRecordingAreIgnored()
    {
        Start();

        _recorder.RecordDamageApplied(-1, true, Alpha, Beta, Damage(1), 10, 9, 100, null!, null!);
        _recorder.RecordDamageApplied(5, true, Alpha, Beta, Damage(1), 10, 9, 100, null!, null!);
        var index = _recorder.BeginDamageApplication(Alpha, Beta, 1, 10);
        _recorder.Stop("x", out _);
        _recorder.RecordDamageApplied(index, true, Alpha, Beta, Damage(1), 10, 9, 100, null!, null!);
        _recorder.EndDamageApplication(99);

        Of(StopAndRead(), "damage").Should().BeEmpty();
    }

    [Test]
    public void EventsRaisedInsideAHit_AreHeldBehindTheDamageEvent()
    {
        Start();
        var index = _recorder.BeginDamageApplication(Alpha, Beta, 10, 100);
        _recorder.RecordResourceChange(Beta, CombatRecordResource.Stamina, 5, 20);
        _recorder.RecordDamageApplied(index, true, Alpha, Beta, Damage(physical: 10), 100, 90, 100, null!, null!);

        var types = StopAndRead().Select(e => e.GetProperty("type").GetString()).ToList();

        types.IndexOf("damage").Should().BeLessThan(types.IndexOf("resource"));
    }

    [Test]
    public void TemporaryHitPointsAbsorbingADamageEvent_AreCreditedToTheirGranter()
    {
        Start();
        using (_recorder.BeginAbilityAttribution(Beta, Strike))
            _recorder.RecordTemporaryHitPointsGranted(Beta, Beta, "shield", 30, 12.5f);
        var before = new[] { new CombatRecordTemporaryHitPointPool { EffectKey = "shield", Amount = 30 } };
        var after = new[] { new CombatRecordTemporaryHitPointPool { EffectKey = "shield", Amount = 10 } };

        ApplyDamage(Alpha, Beta, 20, 100, 100, poolsBefore: before, poolsAfter: after);

        var events = StopAndRead();
        var granted = Of(events, "temp_hp_grant").Single();
        granted.GetProperty("duration_s").GetDouble().Should().Be(12.5);
        granted.GetProperty("amount").GetInt32().Should().Be(30);
        var absorbed = Of(events, "temp_hp_absorb").Single();
        absorbed.GetProperty("amount").GetInt32().Should().Be(20);
        absorbed.GetProperty("granted_name").GetString().Should().Be("Strike");
        Of(events, "damage").Single().GetProperty("temp_hp_absorbed").GetInt32().Should().Be(20);
    }

    [Test]
    public void AbsorbingAPoolNobodyGranted_IsUnattributed()
    {
        Start();
        var before = new[] { new CombatRecordTemporaryHitPointPool { EffectKey = "armor", Amount = 30 } };
        var after = new[] { new CombatRecordTemporaryHitPointPool { EffectKey = "armor", Amount = 0 } };

        ApplyDamage(Alpha, Beta, 30, 100, 100, poolsBefore: before, poolsAfter: after);

        Of(StopAndRead(), "temp_hp_absorb").Single().GetProperty("granted_kind").GetString().Should().Be("unattributed");
    }

    [Test]
    public void PoolRemovalDuringAHit_KeepsWhatThePoolHadLeft()
    {
        Start();
        _recorder.Pools[Beta] = new[] { new CombatRecordTemporaryHitPointPool { EffectKey = "shield", Amount = 25 } };
        var before = new[] { new CombatRecordTemporaryHitPointPool { EffectKey = "shield", Amount = 30 } };
        var index = _recorder.BeginDamageApplication(Alpha, Beta, 5, 100);
        _recorder.RecordTemporaryHitPointPoolRemoving(Beta);
        _recorder.RecordTemporaryHitPointPoolRemoving(Gamma);
        _recorder.RecordDamageApplied(index, true, Alpha, Beta, Damage(physical: 5), 100, 100, 100,
            before, Array.Empty<CombatRecordTemporaryHitPointPool>());

        Of(StopAndRead(), "temp_hp_absorb").Single().GetProperty("amount").GetInt32().Should().Be(5);
    }

    [Test]
    public void TemporaryHitPointTrimsAndUnrecordedGrants_AreHandled()
    {
        Start();
        using (_recorder.BeginAbilityAttribution(Alpha, Strike))
            _recorder.RecordTemporaryHitPointsGranted(Alpha, Beta, "shield", 30, 5f);
        _recorder.RecordTemporaryHitPointsTrimmed(Beta, "shield", 10, 20);
        _recorder.RecordTemporaryHitPointsTrimmed(Beta, "other", 10, 20);
        _recorder.RecordTemporaryHitPointsTrimmed(Beta, "shield", 0, 20);
        _recorder.RecordTemporaryHitPointsTrimmed(Outsider, "shield", 5, 20);
        _recorder.RecordTemporaryHitPointsGranted(Outsider, Outsider, "shield", 30, 5f);
        _recorder.RecordTemporaryHitPointsGranted(ObjectInvalid, Beta, "plain", 10, 1f);

        var events = StopAndRead();
        var trims = Of(events, "temp_hp_trim");
        trims.Should().HaveCount(2);
        trims[0].GetProperty("granted_name").GetString().Should().Be("Strike");
        trims[1].GetProperty("granted_kind").GetString().Should().Be("unattributed");
        Of(events, "temp_hp_grant").Should().HaveCount(2);
    }

    [Test]
    public void Healing_IsRecordedWithItsSourceAndPvpReduction()
    {
        Start();
        _recorder.BeginHealApplication(Beta);
        _recorder.RecordPvpHealingReduced(Beta, 100, 40);
        _recorder.RecordPvpHealingReduced(Gamma, 100, 40);
        var reduced = _recorder.EndHealApplication(Beta);
        using (_recorder.BeginAbilityAttribution(Alpha, Strike))
            _recorder.RecordHealingApplied(Alpha, Beta, 100, 20, 80, 100, reduced);
        _recorder.RecordHealingApplied(Outsider, Outsider, 100, 20, 80, 100);
        using (_recorder.BeginSetupAttribution(Beta))
            _recorder.RecordHealingApplied(Beta, Beta, 100, 20, 80, 100);

        var heal = Of(StopAndRead(), "heal").Single();
        reduced.Should().Be(60);
        heal.GetProperty("requested").GetInt32().Should().Be(40);
        heal.GetProperty("pvp_reduced").GetInt32().Should().Be(60);
        heal.GetProperty("source_name").GetString().Should().Be("Strike");
    }

    [Test]
    public void ReactionsToAHeal_AreWrittenAfterIt()
    {
        Start();
        var reaction = _recorder.BeginHealingReaction(Beta);
        _recorder.RecordResourceChange(Beta, CombatRecordResource.Stamina, 4, 10);
        reaction.Dispose();
        _recorder.RecordHealingApplied(Alpha, Beta, 10, 50, 60, 100);

        var types = StopAndRead().Select(e => e.GetProperty("type").GetString()).ToList();

        types.IndexOf("heal").Should().BeLessThan(types.IndexOf("resource"));
    }

    [Test]
    public void ReactionsToAHealThatNeverComes_AreReleasedWhenTheFrameEnds()
    {
        Start();
        var reaction = _recorder.BeginHealingReaction(Beta);
        _recorder.RecordResourceChange(Beta, CombatRecordResource.Stamina, 4, 10);
        reaction.Dispose();
        _recorder.Frame = 2;
        _recorder.RecordResourceChange(Beta, CombatRecordResource.FP, 1, 10);

        Of(StopAndRead(), "resource").Should().HaveCount(2);
    }

    [Test]
    public void ReactionsStillWaitingWhenTheRecordingStops_AreWrittenFirst()
    {
        Start();
        var reaction = _recorder.BeginHealingReaction(Beta);
        _recorder.RecordResourceChange(Beta, CombatRecordResource.Stamina, 4, 10);
        reaction.Dispose();
        _recorder.RecordHealingApplied(Outsider, Outsider, 1, 1, 1, 1);

        Of(StopAndRead(), "resource").Should().ContainSingle();
    }

    [Test]
    public void Resources_AreRecordedAsChangesReductionsAndItemUse()
    {
        Start();

        _recorder.RecordResourceChange(Alpha, CombatRecordResource.FP, 0, 5);
        _recorder.RecordResourceChange(Outsider, CombatRecordResource.FP, 5, 5);
        _recorder.RecordResourceChange(Alpha, CombatRecordResource.FP, -5, 15);
        _recorder.RecordResourceReduction(Alpha, CombatRecordResource.Stamina, 50, 8);
        _recorder.RecordItemConsumed(Alpha, "medpac", 0);
        _recorder.RecordItemConsumed(Alpha, "medpac", 2);
        _recorder.RecordItemStackReduced(Item, 1);
        using (_recorder.BeginSetupAttribution(Alpha))
        {
            _recorder.RecordItemStackReduced(Item, 1);
            _recorder.RecordItemChargeUsed(Alpha, Item);
            _recorder.RecordItemChargeUsed(Outsider, Item);
        }

        var resources = Of(StopAndRead(), "resource");
        resources.Select(r => r.GetProperty("delta").GetInt32()).Should().Equal(-5, -8, -2, -1, -1);
        resources[1].GetProperty("after").GetInt32().Should().Be(0);
        resources[2].GetProperty("item").GetString().Should().Be("medpac");
        resources[4].GetProperty("reason").GetString().Should().Be("item");
    }

    [Test]
    public void StatusEffects_AreRecordedWhenAppliedAndRemoved()
    {
        Start();
        _recorder.Abilities[FeatType.Alertness] = Strike;
        var status = Status("Burn");

        _recorder.RecordStatusApplied(Alpha, Beta, status, 6f, false, false);
        using (_recorder.BeginAbilityFeatAttribution(Alpha, FeatType.Alertness))
            _recorder.RecordStatusApplied(Alpha, Beta, status, 0f, true, true);
        _recorder.RecordStatusApplied(Outsider, Outsider, status, 6f, false, false);
        _recorder.RecordStatusApplied(Alpha, Beta, null!, 6f, false, false);
        _recorder.RecordStatusRemoved(Beta, status, true);
        _recorder.RecordStatusRemoved(Beta, status, false);
        _recorder.RecordStatusRemoved(Beta, null!, false);

        var events = StopAndRead();
        var applied = Of(events, "status_apply");
        applied.Should().ContainSingle();
        applied[0].GetProperty("duration_s").GetDouble().Should().Be(6);
        applied = Of(events, "status_refresh");
        applied.Should().ContainSingle();
        applied[0].GetProperty("duration_s").ValueKind.Should().Be(JsonValueKind.Null);
        applied[0].GetProperty("ability").GetString().Should().Be("Strike");
        Of(events, "status_remove").Select(e => e.GetProperty("reason").GetString()).Should().Equal("replaced", "removed");
    }

    [Test]
    public void StatusRemovedByExpiry_IsReportedAsExpired()
    {
        Start();
        var status = ScriptedProxy.Create<IStatusEffect>(out var script);
        script.On("get_Name", _ => "Burn");
        script.On("get_Source", _ => Alpha);
        script.On("get_WasNaturallyExpired", _ => true);

        _recorder.RecordStatusRemoved(Beta, status, false);

        Of(StopAndRead(), "status_remove").Single().GetProperty("reason").GetString().Should().Be("expired");
    }

    [Test]
    public void CreatureDeaths_AreRecordedOnceWithTheKillerAndTheLastDamageSource()
    {
        Start();
        ApplyDamage(Alpha, Beta, 5);
        _self = Beta;
        _killer = Gamma;

        _recorder.RecordCreatureDeath();
        _recorder.RecordCreatureDeath();

        var death = Of(StopAndRead(), "death").Single();
        death.GetProperty("killer").GetInt32().Should().BeGreaterThan(0);
        death.GetProperty("last_damage_source").GetInt32().Should().BeGreaterThan(0);
    }

    [Test]
    public void ADeathOutsideTheAreaIsNotRecordedButEndsItsLastDamageSource()
    {
        Start();
        _self = Outsider;

        _recorder.RecordCreatureDeath();

        Of(StopAndRead(), "death").Should().BeEmpty();
    }

    [Test]
    public void DeathHandlers_DoNothingWhileNoRecordingRuns()
    {
        _self = Beta;
        _recorder.RecordCreatureDeath();
        _recorder.RecordPlayerDeath();

        _engine.CallsTo("GetLastKiller").Should().BeEmpty();
        _engine.CallsTo("GetLastPlayerDied").Should().BeEmpty();
    }

    [Test]
    public void PlayerDeath_UsesTheLastHostileActor()
    {
        Start();
        _lastPlayerDied = Alpha;
        _lastHostile = Beta;

        _recorder.RecordPlayerDeath();
        _recorder.RecordRespawn(Alpha);
        _recorder.RecordRespawn(Outsider);
        _recorder.RecordPlayerDeath();

        var events = StopAndRead();
        Of(events, "death").Should().HaveCount(2, "the respawn started a new life");
        Of(events, "resurrection").Single().GetProperty("source_kind").GetString().Should().Be("respawn");
    }

    [Test]
    public void ALethalHit_HoldsTheDeathBehindTheDamageAndKeepsTheHitPointsTheHitLeft()
    {
        Start();
        var index = _recorder.BeginDamageApplication(Alpha, Beta, 100, 100);
        _recorder.HitPoints[Beta] = 0;
        _self = Beta;
        _killer = Alpha;
        _recorder.RecordCreatureDeath();
        _recorder.RecordCreatureDeath();
        _recorder.HitPoints[Beta] = 40;
        _recorder.KeepLethalHitPoints(Beta);
        _recorder.KeepLethalHitPoints(Gamma);
        _recorder.RecordDamageApplied(index, true, Alpha, Beta, Damage(physical: 100), 100, 40, 100, null!, null!);

        var events = StopAndRead();
        var types = events.Select(e => e.GetProperty("type").GetString()).ToList();
        types.IndexOf("damage").Should().BeLessThan(types.IndexOf("death"));
        Of(events, "damage").Single().GetProperty("hp_after").GetInt32().Should().Be(0);
        Of(events, "death").Should().ContainSingle();
    }

    [Test]
    public void ANestedHitOnTheSameCreature_EndsWhatTheOuterOneLeft()
    {
        Start();
        _recorder.HitPoints[Beta] = 70;
        var outer = _recorder.BeginDamageApplication(Alpha, Beta, 10, 100);
        var inner = _recorder.BeginDamageApplication(Gamma, Beta, 5, 90);
        _recorder.RecordHealingApplied(Alpha, Beta, 5, 90, 95, 100);
        _recorder.RecordDamageApplied(inner, true, Gamma, Beta, Damage(5), 90, 85, 100, null!, null!);
        _recorder.RecordDamageApplied(outer, true, Alpha, Beta, Damage(10), 100, 90, 100, null!, null!);

        Of(StopAndRead(), "damage").Should().HaveCount(2);
    }

    [Test]
    public void Resurrections_AreAttributedToTheOpenScopeOrTheGivenKind()
    {
        Start();

        using (_recorder.BeginAbilityAttribution(Alpha, Strike))
            _recorder.RecordResurrection(Beta);
        _recorder.RecordResurrection(Beta, CombatRecordSourceKind.DungeonMaster);
        _recorder.RecordResurrection(Outsider);

        var resurrections = Of(StopAndRead(), "resurrection");
        resurrections.Should().HaveCount(2);
        resurrections[0].GetProperty("source_name").GetString().Should().Be("Strike");
        resurrections[1].GetProperty("source_kind").GetString().Should().Be("dm");
    }

    [Test]
    public void ASubdual_RecordsTheDeathTheSubdualAndTheResurrection()
    {
        Start();
        _lastHostile = Alpha;

        _recorder.RecordSubdual(Beta, Alpha);
        _recorder.RecordSubdual(Outsider, Alpha);

        var events = StopAndRead();
        Of(events, "death").Should().ContainSingle();
        Of(events, "subdual").Should().ContainSingle();
        Of(events, "resurrection").Single().GetProperty("source_kind").GetString().Should().Be("subdual");
    }

    [Test]
    public void ASubdualInsideALethalHit_KeepsTheHitPointsTheHitLeft()
    {
        Start();
        var index = _recorder.BeginDamageApplication(Alpha, Beta, 100, 100);
        _recorder.HitPoints[Beta] = 0;

        _recorder.RecordSubdual(Beta, Alpha);
        _recorder.HitPoints[Beta] = 100;
        _recorder.RecordDamageApplied(index, true, Alpha, Beta, Damage(physical: 100), 100, 100, 100, null!, null!);

        Of(StopAndRead(), "damage").Single().GetProperty("hp_after").GetInt32().Should().Be(0);
    }

    [Test]
    public void ParticipantsAreRegisteredOncePerRecordingAndOnlyForCreatures()
    {
        Start();

        _recorder.RecordAttack(Beta, Gamma, 1, 50, null!);
        _recorder.RecordAttack(Beta, Gamma, 1, 50, null!);
        _recorder.RecordAttack(Item, Beta, 1, 50, null!);
        _recorder.RecordAttack(0, Beta, 1, 50, null!);

        var events = StopAndRead();
        Of(events, "participant").Select(e => e.GetProperty("name").GetString()).Should().Equal("name-1", "name-2", "name-3");
        Of(events, "participant").Last().GetProperty("resref").GetString().Should().Be("res-3");
        Of(events, "attack").Should().HaveCount(4);
    }

    [Test]
    public void ANewRecording_StartsWithCleanState()
    {
        Start();
        using (_recorder.BeginAttribution(Alpha, CombatRecordSourceKind.Item))
        {
            _recorder.ExpectDeferredDamage(Alpha, Beta, 5, CombatDamageDeliveryType.Transferred);
            _recorder.BeginDamageApplication(Alpha, Beta, 1, 10);
            _recorder.Stop("one", out _);
            Start();
        }

        _recorder.HasPendingDamage(Alpha, Beta).Should().BeFalse();
        _recorder.CaptureScope().Actor.Should().Be(0);
        _recorder.GetStatus().Should().Contain("participants");
        _recorder.LastWriter.Should().NotBeNull();
    }

    [Test]
    public void TheDefaultSeamsReadTheirOriginalSources()
    {
        var plain = new PlainRecorder(ScriptedProxy.Create<IDMPresenceService>(out _), new InMemoryDBService());

        plain.CallFrame().Should().Be(0UL);
        plain.CallMillis().Should().BeGreaterThan(0);
        plain.CallUtcNow().Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        plain.CallWrite();
        plain.CallLogDirectory();
    }

    private sealed class PlainRecorder : CombatRecorderService
    {
        public PlainRecorder(IDMPresenceService presence, IDBService db)
            : base(presence, db, FacadeForwarder.LazyFor<IAbilityService>(), FacadeForwarder.LazyFor<IStatService>(), FacadeForwarder.LazyFor<ICombatQueriesService>())
        {
        }

        public ulong CallFrame() => CurrentFrame();
        public long CallMillis() => UnixTimeMilliseconds();
        public DateTime CallUtcNow() => UtcNow();
        public void CallWrite() => WriteLog(LogGroup.DM, "default seam");
        public string CallLogDirectory() => GetLogDirectory();
    }

    private sealed class ScheduledTask : IDisposable
    {
        public ScheduledTask(Action action) => Action = action;
        public Action Action { get; }
        public bool Disposed { get; private set; }
        public void Dispose() => Disposed = true;
    }

    private sealed class Probe : CombatRecorderService
    {
        private readonly string _logDirectory;

        public Probe(IDMPresenceService presence, IDBService db, string logDirectory)
            : base(presence, db, FacadeForwarder.LazyFor<IAbilityService>(), FacadeForwarder.LazyFor<IStatService>(), FacadeForwarder.LazyFor<ICombatQueriesService>())
        {
            _logDirectory = logDirectory;
        }

        public int Hooks { get; private set; }
        public Exception? HookFailure { get; set; }
        public Dictionary<FeatType, AbilityDetail> Abilities { get; } = new();
        public Dictionary<uint, AbilityImpactSummary> ActiveImpacts { get; } = new();
        public Dictionary<uint, int> Fp { get; } = new();
        public Dictionary<uint, int> Stamina { get; } = new();
        public Dictionary<uint, int> HitPoints { get; } = new();
        public Dictionary<uint, CombatRecordTemporaryHitPointPool[]> Pools { get; } = new();
        public List<string> WriteLines { get; } = new();
        public ScheduledTask? Scheduled { get; private set; }
        public ulong Frame { get; set; }
        public long Millis { get; set; } = 1_700_000_000_000;

        protected override void InstallEffectHooks()
        {
            if (HookFailure != null)
                throw HookFailure;
            Hooks++;
        }

        protected override IReadOnlyDictionary<FeatType, AbilityDetail> GetAllAbilityDetails() => Abilities;
        protected override bool IsFeatRegistered(FeatType feat) => Abilities.ContainsKey(feat);
        protected override AbilityDetail GetAbilityDetail(FeatType feat) => Abilities[feat];
        protected override AbilityImpactSummary GetActiveAbilityImpactSummary(uint creature) => ActiveImpacts.GetValueOrDefault(creature)!;
        protected override SkillType GetEquippedWeaponSkillType(uint creature) => SkillType.Lightsaber;
        protected override int GetCurrentFP(uint creature, Player dbPlayer = null!) => Fp.GetValueOrDefault(creature, 11);
        protected override int GetMaxFP(uint creature, Player dbPlayer = null!) => 22;
        protected override int GetCurrentStamina(uint creature, Player dbPlayer = null!) => Stamina.GetValueOrDefault(creature, 33);
        protected override int GetMaxStamina(uint creature, Player dbPlayer = null!) => 44;
        protected override int GetTemporaryHitPoints(uint creature) => 7;
        protected override int GetHitPointsExcludingTemporary(uint creature) => HitPoints.GetValueOrDefault(creature, 80);
        protected override CombatRecordTemporaryHitPointPool[] CaptureTemporaryHitPointPools(uint creature) =>
            Pools.GetValueOrDefault(creature)!;
        protected override string GetLogDirectory() => _logDirectory;

        protected override IDisposable ScheduleRepeating(Action task, TimeSpan schedule, TimeSpan delay)
        {
            Scheduled = new ScheduledTask(task);
            return Scheduled;
        }

        protected override void WriteLog(LogGroup group, string details, bool printToConsole = false) => WriteLines.Add(details);
        protected override ulong CurrentFrame() => Frame;
        protected override long UnixTimeMilliseconds() => Millis;
    }
}
