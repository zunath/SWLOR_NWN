using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;
using SWLOR.NWN.API.NWScript.Enum;
using static SWLOR.Game.Server.Tests.Support.CombatTranscriptBench;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The recorder transcript of a scripted fight (P5-7b), taken before <c>CombatRecorderService</c> is split into layers: a
/// <c>Start</c>, an ability cast with its cost, attack, damage with temporary hit points and impact, a denied, a queued and an
/// interrupted ability, a heal with a nested heal and a reduced heal, temporary hit point grants and trims, resources and items, a
/// status applied, refreshed and removed, a creature death, a player death, a respawn, a resurrection, a subdual, and a <c>Stop</c>.
/// The transcript lists, for each step, what the call returned and every line the writer wrote to the events file, byte for byte,
/// then the log lines the recorder wrote. The summary file is compared too: the writer stamps it through <c>Clock</c>. The one double is <c>Probe</c>, which overrides the protected seams the recorder offers (the clock, the frame, the log
/// folder, the native hooks and the stat reads). Regenerate with <c>SWLOR_UPDATE_SNAPSHOTS=1</c> only for a deliberate change.
/// </summary>
[TestFixture]
[NonParallelizable]
public class CombatRecorderTranscriptTests
{
    private const uint AreaOne = 900;
    private const uint AreaTwo = 901;
    private const uint Outsider = Gone;
    private const uint ItemId = 50;
    private const uint ObjectInvalid = 0x7F000000;
    private const string FixturePath = "SWLOR.Game.Server.Tests/Fixtures/CombatTranscripts/Recorder.txt";

    private static AbilityDetail Strike => new() { Name = "Strike", AbilityLevel = 3, ActivationType = AbilityActivationType.Casted };

    private sealed class Probe : CombatRecorderService
    {
        private readonly string _logDirectory;
        private readonly List<string> _lines;

        public Probe(IDMPresenceService presence, IDBService db, string logDirectory, List<string> lines)
            : base(presence, db, FacadeForwarder.For<IAbilityCatalog>(), FacadeForwarder.For<IAbilityImpactState>(),
                FacadeForwarder.LazyFor<IStatService>(), FacadeForwarder.LazyFor<ICombatQueriesService>())
        {
            _logDirectory = logDirectory;
            _lines = lines;
        }

        public ulong Frame { get; set; }

        public long Millis { get; set; } = 1_700_000_000_000;

        public DateTime Moment { get; set; } = new(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc);

        public Action? Scheduled { get; private set; }

        protected override void InstallEffectHooks()
        {
        }

        protected override IReadOnlyDictionary<FeatType, AbilityDetail> GetAllAbilityDetails() => new Dictionary<FeatType, AbilityDetail>();
        protected override bool IsFeatRegistered(FeatType feat) => false;
        protected override AbilityImpactSummary GetActiveAbilityImpactSummary(uint creature) => null!;
        protected override SkillType GetEquippedWeaponSkillType(uint creature) => SkillType.Lightsaber;
        protected override int GetCurrentFP(uint creature, Player dbPlayer = null!) => 11;
        protected override int GetMaxFP(uint creature, Player dbPlayer = null!) => 22;
        protected override int GetCurrentStamina(uint creature, Player dbPlayer = null!) => 33;
        protected override int GetMaxStamina(uint creature, Player dbPlayer = null!) => 44;
        protected override int GetTemporaryHitPoints(uint creature) => 7;
        protected override int GetHitPointsExcludingTemporary(uint creature) => 80;
        protected override CombatRecordTemporaryHitPointPool[] CaptureTemporaryHitPointPools(uint creature) => null!;
        protected override string GetLogDirectory() => _logDirectory;

        protected override IDisposable ScheduleRepeating(Action task, TimeSpan schedule, TimeSpan delay)
        {
            Scheduled = task;
            _lines.Add($"schedule every {schedule.TotalSeconds}s after {delay.TotalSeconds}s");
            return new Cancel(_lines);
        }

        protected override void WriteLog(LogGroup group, string details, bool printToConsole = false) => _lines.Add($"log {group}: {details}");
        protected override ulong CurrentFrame() => Frame;
        protected override long UnixTimeMilliseconds() => Millis;
        protected override DateTime UtcNow() => Moment;

        private sealed class Cancel : IDisposable
        {
            private readonly List<string> _lines;

            public Cancel(List<string> lines) => _lines = lines;

            public void Dispose() => _lines.Add("schedule cancelled");
        }
    }

    private static IStatusEffect Status(StatusEffectBase effect, uint source)
    {
        effect.ReassignSource(source);
        return effect;
    }

    [Test]
    public void AScriptedFightProducesTheCapturedRecording()
    {
        var directory = Path.Combine(Path.GetTempPath(), "mr-p5-7b-recorder-" + Guid.NewGuid().ToString("N"));
        Probe? probe = null;
        try
        {
            var presence = ScriptedProxy.Create<IDMPresenceService>(out var presenceScript);
            presenceScript.On("IsControlledByDM", _ => false);
            using var bench = new CombatTranscriptBench(Array.Empty<Type>(), (services, host) =>
            {
                probe = new Probe(presence, host.Db, directory + "/", host.Lines);
                services.Replace(ServiceDescriptor.Singleton<ICombatRecorderService>(probe));
            });
            var recorder = bench.Get<ICombatRecorderService>();
            recorder.Should().BeSameAs(probe);
            bench.NameCreature(AreaOne, "Cantina");
            bench.NameCreature(AreaTwo, "Street");
            bench.NameCreature(ItemId, "Item");
            ConfigureEngine(bench);

            var self = ObjectInvalid;
            var killer = ObjectInvalid;
            var lastPlayerDied = ObjectInvalid;
            var lastHostile = ObjectInvalid;
            bench.Engine.SetReturnHandler("OBJECT_SELF", _ => self);
            bench.Engine.SetReturnHandler("GetLastKiller", _ => killer);
            bench.Engine.SetReturnHandler("GetLastPlayerDied", _ => lastPlayerDied);
            bench.Engine.SetReturnHandler("GetLastHostileActor", _ => lastHostile);
            bench.Engine.SetReturnSequence("GetFirstPC", Attacker);
            var location = EngineStructures.Create<Location>(5);
            bench.Engine.SetReturnHandler("GetAreaFromLocation", _ => AreaOne);
            bench.Engine.SetReturnHandler("GetPositionFromLocation", _ => new System.Numerics.Vector3(1.234f, 2f, 3f));

            var statusBleed = Status(new ExposedStatusEffect(), Attacker);
            var statusShield = Status(new AbsoluteDefenseStatusEffect(), Defender);
            var shieldBefore = new[] { new CombatRecordTemporaryHitPointPool { EffectKey = "shield", Amount = 30 } };
            var shieldAfter = new[] { new CombatRecordTemporaryHitPointPool { EffectKey = "shield", Amount = 10 } };
            var physical = new CombatRecordDamageBreakdown { Physical = 40 };

            void Fight(string name, Action action) => bench.Step(name, () =>
            {
                probe!.Frame += 2;
                probe.Millis += 100;
                action();
            });

            Fight("start recording in the cantina", () =>
            {
                var started = probe!.Start(AreaOne, "scripted fight", out var message);
                bench.Lines.Add($"Start returned {started}: {message}");
                bench.Lines.Add($"IsRecording {probe.IsRecording} RecordedArea {bench.Name(probe.RecordedArea)}");
            });
            Fight("a second start is refused", () =>
            {
                var started = probe!.Start(AreaOne, "again", out var message);
                bench.Lines.Add($"Start returned {started}: {message}");
            });
            // The status counts the events the writer thread has written so far, which depends on its timing: only its start is in the text.
            Fight("status of the recording", () => bench.Lines.Add("GetStatus starts: " + probe!.GetStatus().Split(':')[0]));
            Fight("ability cast, cost, attack and expected damage", () =>
            {
                using (probe!.BeginAbilityAttribution(Attacker, Strike, FeatType.Alertness))
                {
                    probe.RecordAbilityCast(Attacker, Defender, location, Strike, FeatType.Alertness, 1.5f);
                    var cost = probe.BeginAbilityCost(Attacker, Strike, FeatType.Alertness);
                    cost.Dispose();
                    probe.RecordAttack(Attacker, Defender, 2, 88, "Strike");
                    probe.ExpectDeferredDamage(Attacker, Defender, 40, CombatDamageDeliveryType.Direct);
                    bench.Lines.Add("HasPendingDamage " + probe.HasPendingDamage(Attacker, Defender));
                }
            });
            Fight("temporary hit points granted, then damage that they absorb", () =>
            {
                using (probe!.BeginAbilityAttribution(Defender, Strike, FeatType.Alertness))
                    probe.RecordTemporaryHitPointsGranted(Defender, Defender, "shield", 30, 12.5f);
                using (probe.BeginAbilityAttribution(Attacker, Strike, FeatType.Alertness))
                {
                    var index = probe.BeginDamageApplication(Attacker, Defender, 40, 100);
                    probe.RecordDamageApplied(index, true, Attacker, Defender, physical, 100, 90, 200, shieldBefore, shieldAfter);
                }
            });
            Fight("temporary hit points trimmed", () => probe!.RecordTemporaryHitPointsTrimmed(Defender, "shield", 5, 5));
            Fight("ability impact", () =>
            {
                var summary = new AbilityImpactSummary
                {
                    Ability = Strike, ImpactedTargetCount = 2, CriticalHitCount = 1, AttributedDamage = 40,
                    ImpactedTargets = new[] { Defender, Other }
                };
                probe!.RecordAbilityImpact(Attacker, Defender, location, Strike, FeatType.Alertness, summary);
                probe.RecordRecurringAbilityImpact(Attacker, Strike, summary);
            });
            Fight("denied, queued and interrupted abilities", () =>
            {
                probe!.RecordAbilityDenied(Attacker, Defender, Strike, FeatType.Alertness, "Not enough FP", true);
                probe.RecordAbilityQueued(Attacker, Defender, Strike, FeatType.Alertness);
                probe.RecordAbilityInterrupted(Attacker, Strike, CombatRecordInterruptCause.Moved);
                probe.RecordAbilityInterrupted(Attacker, Strike, CombatRecordInterruptCause.Damage, Defender);
                probe.NoteToggleDeactivated(Attacker);
                bench.Lines.Add("TakeToggleDeactivation " + probe.TakeToggleDeactivation(Attacker));
            });
            Fight("a heal with a pvp reduction and a nested heal", () =>
            {
                probe!.BeginHealApplication(Defender);
                probe.RecordPvpHealingReduced(Defender, 100, 40);
                probe.BeginHealApplication(Defender);
                probe.RecordPvpHealingReduced(Defender, 50, 25);
                var inner = probe.EndHealApplication(Defender);
                var outer = probe.EndHealApplication(Defender);
                bench.Lines.Add($"inner reduction {inner}, outer reduction {outer}");
                using (probe.BeginAbilityAttribution(Other, Strike, FeatType.Alertness))
                    probe.RecordHealingApplied(Other, Defender, 100, 20, 80, 200, outer);
            });
            Fight("a healing reaction is written after the heal", () =>
            {
                var reaction = probe!.BeginHealingReaction(Defender);
                probe.RecordResourceChange(Defender, CombatRecordResource.Stamina, 4, 10);
                reaction.Dispose();
                probe.RecordHealingApplied(Attacker, Defender, 10, 50, 60, 200);
            });
            Fight("resources and items", () =>
            {
                probe!.RecordResourceChange(Attacker, CombatRecordResource.FP, -5, 15);
                probe.RecordResourceReduction(Attacker, CombatRecordResource.Stamina, 50, 8);
                probe.RecordItemConsumed(Attacker, "medpac", 2);
                probe.RecordItemStackReduced(ItemId, 1);
                probe.RecordItemChargeUsed(Attacker, ItemId);
                using (probe.BeginSetupAttribution(Attacker))
                    probe.RecordResourceChange(Attacker, CombatRecordResource.FP, 3, 18);
            });
            Fight("a status applied, refreshed and removed", () =>
            {
                probe!.RecordStatusApplied(Attacker, Defender, statusBleed, 6f, false, false);
                probe.RecordStatusApplied(Attacker, Defender, statusBleed, 6f, false, true);
                probe.RecordStatusApplied(Defender, Defender, statusShield, 0f, true, false);
                probe.RecordStatusRemoved(Defender, statusBleed, false);
                probe.RecordStatusRemoved(Defender, statusShield, true);
            });
            Fight("something outside the recorded area is ignored", () =>
            {
                probe!.RecordResourceChange(Outsider, CombatRecordResource.FP, -5, 15);
                probe.RecordAbilityCast(Outsider, Outsider, location, Strike, FeatType.Alertness, 1f);
            });
            Fight("a snapshot tick", () => probe!.Scheduled?.Invoke());
            Fight("a creature dies", () =>
            {
                self = Defender;
                killer = Attacker;
                probe!.RecordCreatureDeath();
                probe.RecordCreatureDeath();
            });
            Fight("a player dies", () =>
            {
                lastPlayerDied = Attacker;
                lastHostile = Defender;
                probe!.RecordPlayerDeath();
            });
            Fight("the player respawns and the creature is resurrected", () =>
            {
                probe!.RecordRespawn(Attacker);
                probe.RecordResurrection(Defender, CombatRecordSourceKind.DungeonMaster);
            });
            Fight("a subdual", () =>
            {
                lastPlayerDied = Attacker;
                probe!.RecordSubdual(Attacker, Defender);
            });
            Fight("stop recording", () =>
            {
                var stopped = probe!.Stop("scripted stop", out var message);
                bench.Lines.Add($"Stop returned {stopped}: {message}");
                bench.Lines.Add($"IsRecording {probe.IsRecording}");
                probe.LastWriter!.Completion.Wait(TimeSpan.FromSeconds(10)).Should().BeTrue("the writer finishes its files");
            });
            bench.Step("the events file, line by line", () =>
            {
                var file = File.ReadAllLines(probe!.LastWriter!.EventsPath);
                bench.Lines.Add("events file has " + file.Length + " lines");
                for (var index = 0; index < file.Length; index++)
                    bench.Lines.Add($"{index + 1}: {file[index]}");
            });

            bench.Step("the summary file, line by line", () =>
            {
                var summary = File.ReadAllLines(probe!.LastWriter!.SummaryPath);
                bench.Lines.Add("summary file has " + summary.Length + " lines");
                for (var index = 0; index < summary.Length; index++)
                    bench.Lines.Add($"{index + 1}: {summary[index]}");
            });

            // The recordings folder is a temporary folder named for the run; its name is the one value left out of the text.
            AssertGolden(FixturePath, System.Text.RegularExpressions.Regex.Replace(bench.Text, @"[^ ]*mr-p5-7b-recorder-[0-9a-f]{32}", "<recordings folder>"));
        }
        finally
        {
            try
            {
                if (probe is { IsRecording: true })
                    probe.Stop("teardown", out _);
                probe?.LastWriter?.Completion.Wait(TimeSpan.FromSeconds(10));
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
            catch (Exception exception) when (exception is IOException or AggregateException)
            {
                // A recording that failed is the point of the assertion above.
            }
        }
    }

    /// <summary>The bench engine, set up as a world of two areas: the creatures stand in the first, the outsider in the second.</summary>
    private static void ConfigureEngine(CombatTranscriptBench bench)
    {
        var areas = new Dictionary<uint, uint>
        {
            [Attacker] = AreaOne, [Defender] = AreaOne, [Other] = AreaOne, [Ally] = AreaOne, [Master] = AreaOne, [Outsider] = AreaTwo,
            [AreaOne] = AreaOne, [AreaTwo] = AreaTwo
        };
        var creatures = new HashSet<uint> { Attacker, Defender, Other, Ally, Master, Outsider };
        bench.Engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! is not (0 or 0x7F000000));
        bench.Engine.SetReturnHandler("GetArea", args => areas.GetValueOrDefault((uint)args[0]!, 0x7F000000u));
        bench.Engine.SetReturnHandler("GetObjectType", args => creatures.Contains((uint)args[0]!) ? ObjectType.Creature : ObjectType.Item);
        bench.Engine.SetReturnHandler("GetIsPC", args => (uint)args[0]! == Attacker);
        bench.Engine.SetReturn("GetIsDM", false);
        bench.Engine.SetReturn("GetIsDMPossessed", false);
        bench.Engine.SetReturnHandler("GetResRef", args => "res-" + bench.Name((uint)args[0]!));
        bench.Engine.SetReturnHandler("GetItemPossessor", _ => Attacker);
        bench.Engine.SetReturn("GetIsInCombat", true);
    }
}
