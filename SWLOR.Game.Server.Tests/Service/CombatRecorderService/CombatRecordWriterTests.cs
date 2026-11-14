using System.IO;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Tests.Service.CombatRecorderService;

public class CombatRecordWriterTests
{
    private string _directory;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), "swlor-combat-recorder-tests", Guid.NewGuid().ToString("N"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);
    }

    [Test]
    public async Task Complete_WritesEveryQueuedEventAsJsonLinesAndTheSummary()
    {
        var eventsPath = Path.Combine(_directory, "CombatRecordings", "20261012T231507Z-test.jsonl");
        var summaryPath = Path.Combine(_directory, "CombatRecordings", "20261012T231507Z-test.summary.json");
        var writer = new CombatRecordWriter(eventsPath, summaryPath);
        writer.Start();

        var started = CombatRecordEvent.RecordingStarted("test", "Arena", "arena_01");
        started.TimestampMs = 1760300000000;
        writer.Enqueue(started);
        writer.Enqueue(Stamp(CombatRecordEvent.Participant(1, "Healer", true, null), 1));
        writer.Enqueue(Stamp(CombatRecordEvent.Participant(2, "Target", false, "nw_rat001"), 2));
        for (var index = 0; index < 500; index++)
        {
            writer.Enqueue(Stamp(CombatRecordEvent.Healing(1, 2, CombatRecordSourceKind.Ability, "Benevolence I", 0, 10, 50, 58, 100), 3 + index));
        }
        writer.Enqueue(Stamp(CombatRecordEvent.RecordingStopped("test", 504), 600));
        writer.Complete();

        var summary = await writer.Completion.WaitAsync(TimeSpan.FromSeconds(10));

        writer.Error.Should().BeNull();
        writer.EventsWritten.Should().Be(504);
        var lines = await File.ReadAllLinesAsync(eventsPath);
        lines.Should().HaveCount(504);
        foreach (var line in lines)
        {
            using var _ = JsonDocument.Parse(line);
        }

        summary.GetParticipant(1).HealingDone["Benevolence I"].Effective.Should().Be(500 * 8);
        summary.GetParticipant(2).HealingReceived["Benevolence I"].Overheal.Should().Be(500 * 2);

        using var summaryJson = JsonDocument.Parse(await File.ReadAllTextAsync(summaryPath));
        summaryJson.RootElement.GetProperty("events").GetInt32().Should().Be(504);
        summaryJson.RootElement.GetProperty("participants").GetArrayLength().Should().Be(2);
    }

    [Test]
    public async Task ExistingFile_FailsTheRecordingWithoutOverwritingIt()
    {
        Directory.CreateDirectory(_directory);
        var eventsPath = Path.Combine(_directory, "taken.jsonl");
        await File.WriteAllTextAsync(eventsPath, "keep");
        var writer = new CombatRecordWriter(eventsPath, Path.Combine(_directory, "taken.summary.json"));
        writer.Start();
        writer.Enqueue(CombatRecordEvent.RecordingStarted("test", "Arena", "arena_01"));
        writer.Complete();

        var completion = async () => await writer.Completion.WaitAsync(TimeSpan.FromSeconds(10));

        await completion.Should().ThrowAsync<IOException>();
        writer.Error.Should().NotBeNull();
        (await File.ReadAllTextAsync(eventsPath)).Should().Be("keep");
    }


    private static CombatRecordEvent[] FixtureEvents()
    {
        var started = CombatRecordEvent.RecordingStarted("test", "Arena", "arena_01");
        started.TimestampMs = 1760300000000;
        return new[]
        {
            started,
            Stamp(CombatRecordEvent.Participant(1, "Healer", true, null), 1),
            Stamp(CombatRecordEvent.Participant(2, "Target", false, "nw_rat001"), 2),
            Stamp(CombatRecordEvent.Healing(1, 2, CombatRecordSourceKind.Ability, "Benevolence I", 0, 10, 50, 58, 100), 3),
            Stamp(CombatRecordEvent.Healing(1, 2, CombatRecordSourceKind.Ability, "Benevolence I", 0, 10, 58, 60, 100), 4),
            Stamp(CombatRecordEvent.RecordingStopped("test", 5), 5)
        };
    }

    [Test]
    public async Task Complete_WritesTheRecordedFixtureByteForByteAsBefore()
    {
        var eventsPath = Path.Combine(_directory, "fixture.jsonl");
        var writer = new CombatRecordWriter(eventsPath, Path.Combine(_directory, "fixture.summary.json"));
        writer.Start();
        foreach (var recordEvent in FixtureEvents())
            writer.Enqueue(recordEvent);
        writer.Complete();
        await writer.Completion.WaitAsync(TimeSpan.FromSeconds(10));

        (await File.ReadAllTextAsync(eventsPath)).Should().Be(GoldenEvents);
    }

    [Test]
    public async Task ConcurrentProducers_KeepEachProducersOrder()
    {
        var eventsPath = Path.Combine(_directory, "concurrent.jsonl");
        var writer = new CombatRecordWriter(eventsPath, Path.Combine(_directory, "concurrent.summary.json"));
        writer.Start();
        const int producers = 4;
        const int perProducer = 250;
        await Task.WhenAll(Enumerable.Range(0, producers).Select(producer => Task.Run(() =>
        {
            for (var index = 0; index < perProducer; index++)
                writer.Enqueue(Stamp(CombatRecordEvent.Healing(producer + 1, 99, CombatRecordSourceKind.Ability, "Benevolence I", 0, 10, 50, 58, 100), index));
        })));
        writer.Complete();
        await writer.Completion.WaitAsync(TimeSpan.FromSeconds(10));

        var lines = await File.ReadAllLinesAsync(eventsPath);
        lines.Should().HaveCount(producers * perProducer);
        var stamps = new Dictionary<long, List<long>>();
        foreach (var line in lines)
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;
            var source = root.GetProperty("source").GetInt64();
            if (!stamps.TryGetValue(source, out var list))
                stamps[source] = list = new List<long>();
            list.Add(root.GetProperty("ts_ms").GetInt64());
        }

        stamps.Should().HaveCount(producers);
        foreach (var list in stamps.Values)
            list.Should().BeInAscendingOrder();
    }

    [Test]
    public async Task StopAsync_DrainsEveryQueuedEventBeforeItReturns()
    {
        var eventsPath = Path.Combine(_directory, "stop.jsonl");
        var summaryPath = Path.Combine(_directory, "stop.summary.json");
        var writer = new CombatRecordWriter(eventsPath, summaryPath);
        writer.Start();
        foreach (var recordEvent in FixtureEvents())
            writer.Enqueue(recordEvent);

        await writer.StopAsync().WaitAsync(TimeSpan.FromSeconds(10));

        writer.EventsWritten.Should().Be(6);
        (await File.ReadAllLinesAsync(eventsPath)).Should().HaveCount(6);
        File.Exists(summaryPath).Should().BeTrue();
    }

    [Test]
    public void WaitForExit_ReturnsOnceTheQueueIsDrained()
    {
        var eventsPath = Path.Combine(_directory, "wait.jsonl");
        var writer = new CombatRecordWriter(eventsPath, Path.Combine(_directory, "wait.summary.json"));
        writer.Start();
        foreach (var recordEvent in FixtureEvents())
            writer.Enqueue(recordEvent);
        writer.Complete();

        writer.WaitForExit(TimeSpan.FromSeconds(10)).Should().BeTrue();
        writer.EventsWritten.Should().Be(6);
    }

    [Test]
    public async Task Writer_StartsNoDedicatedThread()
    {
        var writer = new CombatRecordWriter(Path.Combine(_directory, "threads.jsonl"), Path.Combine(_directory, "threads.summary.json"));
        writer.Start();
        foreach (var recordEvent in FixtureEvents())
            writer.Enqueue(recordEvent);

        await writer.StopAsync().WaitAsync(TimeSpan.FromSeconds(10));

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        directory.Should().NotBeNull();
        var source = await File.ReadAllTextAsync(Path.Combine(
            directory!.FullName, "SWLOR.Game.Server", "Domain", "Combat", "CombatRecordWriter.cs"));
        source.Should().NotContain("new Thread");
    }

    // Captured from the dedicated-thread writer before P4-6.
    private const string GoldenEvents =
        "{\"type\":\"recording_started\",\"ts_ms\":1760300000000,\"frame\":0,\"label\":\"test\",\"area_name\":\"Arena\",\"area_resref\":\"arena_01\",\"started_utc\":\"2025-10-12T20:13:20.0000000Z\"}\n" +
        "{\"type\":\"participant\",\"ts_ms\":1760300000001,\"frame\":0,\"id\":1,\"name\":\"Healer\",\"player\":true}\n" +
        "{\"type\":\"participant\",\"ts_ms\":1760300000002,\"frame\":0,\"id\":2,\"name\":\"Target\",\"player\":false,\"resref\":\"nw_rat001\"}\n" +
        "{\"type\":\"heal\",\"ts_ms\":1760300000003,\"frame\":0,\"source\":1,\"target\":2,\"source_kind\":\"ability\",\"source_name\":\"Benevolence I\",\"requested\":10,\"effective\":8,\"overheal\":2,\"pvp_reduced\":0,\"hp_before\":50,\"hp_after\":58,\"max_hp\":100}\n" +
        "{\"type\":\"heal\",\"ts_ms\":1760300000004,\"frame\":0,\"source\":1,\"target\":2,\"source_kind\":\"ability\",\"source_name\":\"Benevolence I\",\"requested\":10,\"effective\":2,\"overheal\":8,\"pvp_reduced\":0,\"hp_before\":58,\"hp_after\":60,\"max_hp\":100}\n" +
        "{\"type\":\"recording_stopped\",\"ts_ms\":1760300000005,\"frame\":0,\"reason\":\"test\",\"events\":5}\n";

    private static CombatRecordEvent Stamp(CombatRecordEvent recordEvent, int offsetMs)
    {
        recordEvent.TimestampMs = 1760300000000 + offsetMs;
        return recordEvent;
    }
}
