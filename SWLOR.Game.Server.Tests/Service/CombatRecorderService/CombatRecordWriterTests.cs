using System.IO;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CombatRecorderService;

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

    private static CombatRecordEvent Stamp(CombatRecordEvent recordEvent, int offsetMs)
    {
        recordEvent.TimestampMs = 1760300000000 + offsetMs;
        return recordEvent;
    }
}
