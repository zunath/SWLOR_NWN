using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Tests.Domain.Social;

/// <summary>
/// Reading the Discord events snapshot, making Discord text safe for the game, and planning what a sync changes.
/// </summary>
[TestFixture]
public class DiscordEventImportTests
{
    private static readonly DateTime Start = new(2026, 11, 21, 19, 0, 0, DateTimeKind.Utc);

    private static string Snapshot(params string[] events) => "{\"events\":[" + string.Join(",", events) + "]}";

    private static string Item(string id, int status = 1, string name = "Movie night", string start = "2026-11-21T19:00:00+00:00",
        string description = "Bring snacks", string global = "Jess", string user = "jess01") =>
        $"{{\"id\":\"{id}\",\"status\":{status},\"name\":\"{name}\",\"description\":\"{description}\"," +
        $"\"scheduled_start_time\":\"{start}\",\"creator\":{{\"global_name\":\"{global}\",\"username\":\"{user}\"}}}}";

    private static DiscordScheduledEvent Discord(string id, string name = "Movie night") => new()
    {
        Id = id, Name = name, Description = "d", HostName = "h", StartUtc = Start,
    };

    [Test]
    public void Parse_ReadsAScheduledEventAsUtc()
    {
        var events = DiscordEventImport.Parse(Snapshot(Item("1", status: 2)))!;

        events.Should().ContainSingle();
        events[0].Id.Should().Be("1");
        events[0].Name.Should().Be("Movie night");
        events[0].HostName.Should().Be("Jess");
        events[0].Description.Should().Be("Bring snacks");
        events[0].StartUtc.Should().Be(Start);
        events[0].StartUtc.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Test]
    public void Parse_SkipsFinishedCancelledAndRepeatedEvents()
    {
        var events = DiscordEventImport.Parse(Snapshot(Item("1"), Item("1"), Item("2", status: 3), Item("3", status: 4), Item("4")))!;

        events.Select(e => e.Id).Should().Equal("1", "4");
    }

    [Test]
    public void Parse_FallsBackToTheUsernameAndThenDefaults()
    {
        var events = DiscordEventImport.Parse(Snapshot(
            Item("1", global: "", user: "jess01", name: ""),
            "{\"id\":\"2\",\"status\":1,\"scheduled_start_time\":\"2026-11-21T19:00:00Z\"}"))!;

        events[0].HostName.Should().Be("jess01");
        events[0].Name.Should().Be(DiscordEventImport.DefaultName);
        events[1].HostName.Should().Be(DiscordEventImport.DefaultHost);
        events[1].Name.Should().Be(DiscordEventImport.DefaultName);
        events[1].Description.Should().BeEmpty();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("not json")]
    [TestCase("[]")]
    [TestCase("{\"other\":1}")]
    [TestCase("{\"events\":[1]}")]
    [TestCase("{\"events\":[{\"status\":1,\"scheduled_start_time\":\"2026-11-21T19:00:00Z\"}]}")]
    [TestCase("{\"events\":[{\"id\":\"1\",\"status\":9,\"scheduled_start_time\":\"2026-11-21T19:00:00Z\"}]}")]
    [TestCase("{\"events\":[{\"id\":\"1\",\"status\":\"1\",\"scheduled_start_time\":\"2026-11-21T19:00:00Z\"}]}")]
    [TestCase("{\"events\":[{\"id\":\"1\",\"status\":1,\"scheduled_start_time\":\"soon\"}]}")]
    public void Parse_IgnoresASnapshotItCannotFullyRead(string json)
    {
        DiscordEventImport.Parse(json).Should().BeNull();
    }

    [Test]
    public void Plan_CreatesNewEventsUpdatesChangedOnesAndRemovesVanishedOnes()
    {
        var unchanged = new HostedEvent { Id = "a", DiscordEventId = "d-same", Name = "Movie night", HostName = "h", Description = "d", ScheduledStartUtc = Start };
        var changed = new HostedEvent { Id = "b", DiscordEventId = "d-changed", Name = "Old", HostName = "h", Description = "d", ScheduledStartUtc = Start };
        var started = new HostedEvent { Id = "c", DiscordEventId = "d-started", Name = "Old", Status = HostedEventStatus.Running, ScheduledStartUtc = Start };
        var gone = new HostedEvent { Id = "d", DiscordEventId = "d-gone", ScheduledStartUtc = Start };
        var startedGone = new HostedEvent { Id = "e", DiscordEventId = "d-startedgone", Status = HostedEventStatus.Running, ScheduledStartUtc = Start };
        var local = new HostedEvent { Id = "f" };

        var plan = DiscordEventImport.Plan(
            new[] { Discord("d-same"), Discord("d-changed", "New"), Discord("d-started", "New"), Discord("d-new") },
            new[] { unchanged, changed, started, gone, startedGone, local },
            new Dictionary<string, DateTime>());

        plan.Create.Select(e => e.Id).Should().Equal("d-new");
        plan.Update.Select(u => u.Event.Id).Should().Equal("b");
        plan.Remove.Select(e => e.Id).Should().Equal("d");
        plan.DropDismissals.Should().BeEmpty();
    }

    [Test]
    public void Plan_HonoursDismissalsForTheSameStartAndDropsStaleOnes()
    {
        var plan = DiscordEventImport.Plan(
            new[] { Discord("d-dismissed"), Discord("d-moved") },
            Array.Empty<HostedEvent>(),
            new Dictionary<string, DateTime>
            {
                ["d-dismissed"] = Start,
                ["d-moved"] = Start.AddDays(-7),
                ["d-deleted"] = Start,
            });

        plan.Create.Select(e => e.Id).Should().Equal("d-moved");
        plan.DropDismissals.Should().BeEquivalentTo("d-moved", "d-deleted");
    }

    [Test]
    public void Plan_ReadsTheFirstOfTwoEventsSharingADiscordId()
    {
        var first = new HostedEvent { Id = "a", DiscordEventId = "d1", Name = "Movie night", HostName = "h", Description = "d", ScheduledStartUtc = Start };
        var second = new HostedEvent { Id = "b", DiscordEventId = "d1", Name = "Other" };

        DiscordEventImport.Plan(new[] { Discord("d1") }, new[] { first, second }, new Dictionary<string, DateTime>())
            .Update.Should().BeEmpty();
    }

    [Test]
    public void Apply_CopiesDiscordsDetailsAndRemembersItsStart()
    {
        var hostedEvent = new HostedEvent();

        DiscordEventImport.Apply(hostedEvent, Discord("d1", "Quiz"));

        hostedEvent.DiscordEventId.Should().Be("d1");
        hostedEvent.Name.Should().Be("Quiz");
        hostedEvent.HostName.Should().Be("h");
        hostedEvent.Description.Should().Be("d");
        hostedEvent.ScheduledStartUtc.Should().Be(Start);
        hostedEvent.DiscordStartUtc.Should().Be(Start);
    }

    [Test]
    public void TheOccurrenceStartSurvivesADMEditingTheTime()
    {
        var hostedEvent = new HostedEvent { ScheduledStartUtc = Start.AddHours(2) };
        DiscordEventImport.GetOccurrenceStart(hostedEvent).Should().Be(Start.AddHours(2));

        hostedEvent.DiscordStartUtc = Start;
        DiscordEventImport.GetOccurrenceStart(hostedEvent).Should().Be(Start);
    }

    [Test]
    public void OnlyAnImportedUnstartedEventIsSyncedFromDiscord()
    {
        DiscordEventImport.IsSyncedFromDiscord(null).Should().BeFalse();
        DiscordEventImport.IsSyncedFromDiscord(new HostedEvent()).Should().BeFalse();
        DiscordEventImport.IsSyncedFromDiscord(new HostedEvent { DiscordEventId = "d1" }).Should().BeTrue();
        DiscordEventImport.IsSyncedFromDiscord(new HostedEvent { DiscordEventId = "d1", Status = HostedEventStatus.Running }).Should().BeFalse();
    }

    [Test]
    public void ToGameText_RemovesDiscordMarkup()
    {
        var text = "# Title\n**bold** __under__ ~~strike~~ ||spoiler|| <@123> <@!4> <@&5> <#6> <:wave:77> <a:dance:88> [site](https://example.com/x) done";

        DiscordEventImport.ToGameText(text, 500, singleLine: false).Should().Be("Title\nbold under strike spoiler site done");
    }

    [Test]
    public void ToGameText_TurnsTimestampsIntoUtcTextAndDropsOutOfRangeOnes()
    {
        DiscordEventImport.ToGameText("At <t:1795287600:F> sharp", 100, true).Should().Be("At 2026-11-21 19:00 UTC sharp");
        DiscordEventImport.ToGameText("At <t:99999999999999> sharp", 100, true).Should().Be("At sharp");
    }

    [Test]
    public void ToGameText_MakesPunctuationAsciiAndKeepsAccentedLetters()
    {
        var text = "‘a’ “b” x–y—z • q·r… café ok\tgo ő 中 😀";

        DiscordEventImport.ToGameText(text, 200, true).Should().Be("'a' \"b\" x-y-z - q-r... café ok go o");
    }

    [Test]
    public void ToGameText_KeepsLinesForDescriptionsAndFlattensThemForNames()
    {
        var text = "one\r\n\r\n\r\n\r\ntwo   spaced\rthree";

        DiscordEventImport.ToGameText(text, 200, singleLine: false).Should().Be("one\n\ntwo spaced\nthree");
        DiscordEventImport.ToGameText(text, 200, singleLine: true).Should().Be("one two spaced three");
    }

    [Test]
    public void ToGameText_CutsLongTextWithAnEllipsis()
    {
        DiscordEventImport.ToGameText(new string('a', 30), 10, true).Should().Be("aaaaaaa...");
        DiscordEventImport.ToGameText(null, 10, true).Should().BeEmpty();
    }
}
