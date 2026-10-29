using System.Diagnostics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.PerformanceService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using Vector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The owner asked that Performance never become a server hog. These guard the audience tick's budget: its decisions
/// are cheap for a full house, and its source keeps the one-schedule, one-pass, one-read-and-write-per-watcher shape.
/// </summary>
public class PerformanceAudienceCostTests
{
    private const int Performers = 10;
    private const int Watchers = 60;
    private const int Iterations = 100;
    private const double MaxAverageMilliseconds = 5.0;

    [Test]
    public void TickDecisions_TenPerformersAndSixtyWatchersCostLessThanFiveMillisecondsAnIteration()
    {
        var performers = BuildPerformers();
        var watchers = BuildWatchers(performers);
        var checksum = RunTickDecisions(performers, watchers);

        var stopwatch = Stopwatch.StartNew();
        for (var iteration = 0; iteration < Iterations; iteration++)
            checksum += RunTickDecisions(performers, watchers);
        stopwatch.Stop();

        var average = stopwatch.Elapsed.TotalMilliseconds / Iterations;
        checksum.Should().BeGreaterThan(0.0);
        average.Should().BeLessThan(MaxAverageMilliseconds);
    }

    // The pure part of one tick for a full house: relate the performers, then choose, plan and compose for each watcher.
    private static double RunTickDecisions(List<PerformerSnapshot> performers, List<List<PerformerSnapshot>> watchers)
    {
        PerformanceShow.Prepare(performers);

        var compositions = new Dictionary<(uint, uint, int), InspiredEffectData>();
        var banked = 0.0;
        foreach (var candidates in watchers)
        {
            var dancer = PerformanceFollow.Choose(candidates, PerformanceKind.Dance, 0, 0);
            var musician = PerformanceFollow.Choose(candidates, PerformanceKind.Music, 0, 0);
            var plan = PerformanceWatchPlan.Create(
                5, dancer, musician, PerformanceAudienceRules.TroupeCount(candidates));

            var key = (dancer?.Performer ?? 0u, musician?.Performer ?? 0u, plan.Venue);
            if (!compositions.TryGetValue(key, out var composition))
            {
                composition = PerformanceInspiredComposer.Compose(dancer, musician, plan.Venue);
                compositions[key] = composition;
            }

            banked += plan.BankedMinutes + composition.XPBonusPercent;
        }

        return banked;
    }

    private static List<PerformerSnapshot> BuildPerformers()
    {
        var performers = new List<PerformerSnapshot>();
        for (var index = 0; index < Performers; index++)
        {
            var isDancer = index % 2 == 0;
            performers.Add(new PerformerSnapshot
            {
                Performer = (uint)(index + 1),
                Kind = isDancer ? PerformanceKind.Dance : PerformanceKind.Music,
                StartedUtc = PerformanceTestSnapshots.Start.AddMinutes(-index),
                Position = new Vector3(index * 1.5f, 0f, 0f),
                XpBase = 12,
                XpScaled = 12 + index % 4,
                Routine = isDancer ? PerformanceRoutineType.PowerStep : PerformanceRoutineType.CantinaShuffle,
                SecondRoutine = isDancer ? PerformanceRoutineType.LightFeet : PerformanceRoutineType.CantinaShuffle,
                Set = isDancer ? PerformanceSetType.CantinaStandards : PerformanceSetType.KeenEar,
                SecondSet = isDancer ? PerformanceSetType.CantinaStandards : PerformanceSetType.SilverTongue,
                HasCapstone = true,
                HasPartnerTrait = true,
                SongTrackId = 100 + index,
                Venue = 3
            });
        }

        return performers;
    }

    private static List<List<PerformerSnapshot>> BuildWatchers(List<PerformerSnapshot> performers)
    {
        var watchers = new List<List<PerformerSnapshot>>();
        for (var index = 0; index < Watchers; index++)
            watchers.Add(new List<PerformerSnapshot>(performers));

        return watchers;
    }

    [Test]
    public void AudienceTick_UsesOneModuleWideScheduleAndProfilesIt()
    {
        var source = File.ReadAllText(SourcePath("Service", "PerformanceService", "PerformanceAudience.cs"));

        CountOf(source, "Scheduler.ScheduleRepeating(").Should().Be(1);
        source.Should().Contain("FrameProfiler.Stop(FrameMetric.Scheduler, profilerStart, \"PerformanceAudienceTick\")");
    }

    [Test]
    public void AudienceTick_FindsWatchersInOnePassOverOnlinePlayersAndReadsThePlayerOncePerWatcher()
    {
        var source = File.ReadAllText(SourcePath("Service", "PerformanceService", "PerformanceAudience.cs"));

        CountOf(source, "GetFirstPC()").Should().Be(1);
        source.Should().NotContain("GetFirstObjectInArea");
        source.Should().NotContain("GetNearestObject");
        CountOf(source, "DB.Get<Player>(").Should().Be(1);
        CountOf(source, "DB.Set(").Should().Be(1);
        CountOf(source, "Skill.GiveSkillXP(").Should().Be(1);
    }

    [Test]
    public void SharedSystems_ReadStatTypesAndNeverNamePerformancePerks()
    {
        var shared = new[]
        {
            SourcePath("Service", "Death.cs"),
            SourcePath("Service", "Fishing.cs"),
            SourcePath("Service", "Stat.cs"),
            SourcePath("Service", "StatusEffect.cs"),
            SourcePath("Service", "Combat.cs")
        };

        foreach (var file in shared)
        {
            var source = File.ReadAllText(file);
            source.Should().NotContain("InspiredStatusEffect", $"{Path.GetFileName(file)} is a shared system");
            source.Should().NotContain("PerkType.Requiem");
            source.Should().NotContain("PerkType.StandingOvation");
            source.Should().NotContain("PerkType.LightFeet");
            source.Should().NotContain("PerkType.AnglersLullaby");
        }

        File.ReadAllText(SourcePath("Service", "Stat.cs"))
            .Should().Contain("StatType.OutOfCombatMovementSpeedPercentAdjustment");
        File.ReadAllText(SourcePath("Service", "Fishing.cs"))
            .Should().Contain("StatType.FishingCatchChancePercentAdjustment");
        File.ReadAllText(SourcePath("Service", "Death.cs")).Should().Contain("IDeathDebtWaiverStatusEffect");
    }

    [Test]
    public void EveryPerformanceStatHasAProductionConsumer()
    {
        var productionRoot = Path.Combine(FindRepositoryRoot(), "SWLOR.Game.Server");
        var corpus = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(productionRoot, "*.cs", SearchOption.AllDirectories)
                .Where(file => !file.Contains(Path.Combine("Feature", "PerkDefinition"), StringComparison.OrdinalIgnoreCase))
                .Where(file => !file.EndsWith(Path.Combine("Service", "StatService", "StatType.cs"), StringComparison.OrdinalIgnoreCase))
                .Select(File.ReadAllText));

        var stats = Enum.GetValues<StatType>().Where(stat => (int)stat >= 1300 && (int)stat <= 1314).ToArray();

        stats.Should().HaveCount(15);
        stats.Where(stat => !corpus.Contains($"StatType.{stat}", StringComparison.Ordinal))
            .Should().BeEmpty("a Performance stat nothing reads is an unimplemented promise");
    }

    private static int CountOf(string source, string text)
    {
        var count = 0;
        for (var index = source.IndexOf(text, StringComparison.Ordinal);
             index >= 0;
             index = source.IndexOf(text, index + text.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }

    private static string SourcePath(params string[] parts)
    {
        return Path.Combine(new[] { FindRepositoryRoot(), "SWLOR.Game.Server" }.Concat(parts).ToArray());
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            directory = directory.Parent;

        directory.Should().NotBeNull("the repository root holds AGENTS.md");
        return directory.FullName;
    }
}
