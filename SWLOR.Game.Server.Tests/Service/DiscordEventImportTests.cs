using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.HostedEventService;

namespace SWLOR.Game.Server.Tests.Service;

public class DiscordEventImportTests
{
    private static readonly DateTime Start = new(2026, 11, 21, 0, 30, 0, DateTimeKind.Utc);
    private static readonly Dictionary<string, DateTime> NoDismissals = new();

    // Shaped like Discord's GET /guilds/{id}/scheduled-events response.
    private static JObject DiscordEvent(
        string id,
        string name = "Cantina Night",
        int status = 1,
        string start = "2026-11-21T00:30:00+00:00",
        string description = "Drinks and music.",
        string globalName = "Zeta",
        string username = "zeta_dm")
    {
        return new JObject
        {
            ["id"] = id,
            ["guild_id"] = "100",
            ["channel_id"] = null,
            ["creator_id"] = "200",
            ["name"] = name,
            ["description"] = description,
            ["scheduled_start_time"] = start,
            ["scheduled_end_time"] = "2026-11-21T03:00:00+00:00",
            ["privacy_level"] = 2,
            ["status"] = status,
            ["entity_type"] = 3,
            ["entity_metadata"] = new JObject { ["location"] = "In game" },
            ["creator"] = new JObject { ["id"] = "200", ["username"] = username, ["global_name"] = globalName },
        };
    }

    private static string Snapshot(params JObject[] events)
    {
        return new JObject { ["fetchedUtc"] = "2026-11-17T12:00:00Z", ["events"] = new JArray(events) }.ToString();
    }

    private static DiscordScheduledEvent Listed(string id, DateTime? start = null, string name = "Cantina Night") => new()
    {
        Id = id,
        Name = name,
        Description = "Drinks and music.",
        HostName = "Zeta",
        StartUtc = start ?? Start,
    };

    private static HostedEvent Imported(string discordId, HostedEventStatus status = HostedEventStatus.Scheduled)
    {
        var hostedEvent = new HostedEvent { Status = status };
        DiscordEventImport.Apply(hostedEvent, Listed(discordId));
        return hostedEvent;
    }

    [Test]
    public void Parse_ReadsScheduledAndActiveEvents()
    {
        var events = DiscordEventImport.Parse(Snapshot(
            DiscordEvent("1"),
            DiscordEvent("2", name: "Swoop Race", status: 2),
            DiscordEvent("3", status: 3),
            DiscordEvent("4", status: 4)));

        events.Select(e => e.Id).Should().Equal("1", "2");
        var first = events[0];
        first.Name.Should().Be("Cantina Night");
        first.Description.Should().Be("Drinks and music.");
        first.HostName.Should().Be("Zeta");
        first.StartUtc.Should().Be(Start);
        first.StartUtc.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Test]
    public void Parse_ConvertsOffsetsToUtc()
    {
        var events = DiscordEventImport.Parse(Snapshot(DiscordEvent("1", start: "2026-11-20T19:30:00-05:00")));

        events.Single().StartUtc.Should().Be(Start);
    }

    [Test]
    public void Parse_UsesTheUsernameWithoutADisplayNameAndDiscordWithoutACreator()
    {
        var noDisplayName = DiscordEvent("1", globalName: null);
        var noCreator = DiscordEvent("2");
        noCreator.Remove("creator");

        var events = DiscordEventImport.Parse(Snapshot(noDisplayName, noCreator));

        events[0].HostName.Should().Be("zeta_dm");
        events[1].HostName.Should().Be(DiscordEventImport.DefaultHost);
    }

    [Test]
    public void Parse_SkipsRepeatedIds()
    {
        var events = DiscordEventImport.Parse(Snapshot(DiscordEvent("3"), DiscordEvent("3", name: "Copy")));

        events.Should().ContainSingle().Which.Name.Should().Be("Cantina Night");
    }

    [Test]
    public void Parse_RejectsTheSnapshotWhenAnEntryCannotBeRead()
    {
        // Each of these would otherwise drop out of the list and remove an event a DM may have set up.
        var noStatus = DiscordEvent("2");
        noStatus.Remove("status");
        var malformed = new JToken[]
        {
            JValue.CreateNull(),
            new JValue("2"),
            DiscordEvent(""),
            DiscordEvent("2", status: 9),
            noStatus,
        };

        foreach (var entry in malformed)
        {
            var snapshot = new JObject { ["fetchedUtc"] = "2026-11-17T12:00:00Z", ["events"] = new JArray(DiscordEvent("1"), entry) };
            DiscordEventImport.Parse(snapshot.ToString()).Should().BeNull(entry.ToString());
        }
    }

    [TestCase(null)]
    [TestCase("next Friday")]
    public void Parse_RejectsTheSnapshotWhenAListedEventHasNoReadableStart(string start)
    {
        // Dropping the entry would remove an event a DM may have set up.
        var unreadable = DiscordEvent("2");
        unreadable["scheduled_start_time"] = start;

        DiscordEventImport.Parse(Snapshot(DiscordEvent("1"), unreadable)).Should().BeNull();
    }

    [Test]
    public void Parse_IgnoresAnUnreadableStartOnAFinishedEvent()
    {
        var finished = DiscordEvent("2", status: 3, start: "not a date");

        DiscordEventImport.Parse(Snapshot(DiscordEvent("1"), finished)).Should().ContainSingle();
    }

    [Test]
    public void Parse_NamesAnEventWhoseNameIsOnlyEmoji()
    {
        DiscordEventImport.Parse(Snapshot(DiscordEvent("1", name: "\U0001F389\U0001F389")))
            .Single().Name.Should().Be(DiscordEventImport.DefaultName);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("not json")]
    [TestCase("[]")]
    [TestCase("{\"fetchedUtc\":\"2026-11-17T12:00:00Z\"}")]
    public void Parse_ReturnsNullWithoutAReadableSnapshot(string snapshot)
    {
        DiscordEventImport.Parse(snapshot).Should().BeNull();
    }

    [Test]
    public void Parse_ReturnsAnEmptyListWhenDiscordHasNoEvents()
    {
        DiscordEventImport.Parse(Snapshot()).Should().BeEmpty();
    }

    [TestCase("It’s “fun” – really…", "It's \"fun\" - really...")]
    [TestCase("Café Détritus", "Café Détritus")]
    [TestCase("Party \U0001F389 time ✨", "Party time")]
    [TestCase("**Bring** __armor__ ~~or not~~ ||secret||", "Bring armor or not secret")]
    [TestCase("Ask <@123456> or <@&789> in <#555> <:sabers:999>", "Ask or in")]
    [TestCase("See [the guide](https://example.com/guide) first", "See the guide first")]
    [TestCase("Starts <t:1763685000:F>", "Starts 2025-11-21 00:30 UTC")]
    [TestCase("Gdańsk Őrs", "Gdansk Ors")]
    [TestCase("日本 Meetup", "Meetup")]
    public void ToGameText_MakesDiscordTextSafeForTheGame(string discord, string expected)
    {
        DiscordEventImport.ToGameText(discord, 200, singleLine: true).Should().Be(expected);
    }

    [Test]
    public void ToGameText_KeepsParagraphsInDescriptions()
    {
        DiscordEventImport.ToGameText("# Cantina Night\r\n\r\n\r\n\r\nBring  a\tfriend.\nNo blasters.", 200, singleLine: false)
            .Should().Be("Cantina Night\n\nBring a friend.\nNo blasters.");
    }

    [Test]
    public void ToGameText_JoinsLinesInNames()
    {
        DiscordEventImport.ToGameText("Cantina\nNight", 64, singleLine: true).Should().Be("Cantina Night");
    }

    [Test]
    public void ToGameText_CutsLongTextToTheLimit()
    {
        var text = DiscordEventImport.ToGameText(new string('a', 100), HostedEventRules.MaxNameLength, singleLine: true);

        text.Should().HaveLength(HostedEventRules.MaxNameLength).And.EndWith("...");
    }

    [Test]
    public void Plan_ListsNewDiscordEvents()
    {
        var plan = DiscordEventImport.Plan(new[] { Listed("1") }, new List<HostedEvent>(), NoDismissals);

        plan.Create.Should().ContainSingle().Which.Id.Should().Be("1");
        plan.Update.Should().BeEmpty();
        plan.Remove.Should().BeEmpty();
    }

    [Test]
    public void Plan_CopiesDiscordChangesToAnEventNoDMHasStarted()
    {
        var hostedEvent = Imported("1");
        var moved = Listed("1", Start.AddHours(2), "Cantina Night II");

        var plan = DiscordEventImport.Plan(new[] { moved }, new[] { hostedEvent }, NoDismissals);

        plan.Create.Should().BeEmpty();
        plan.Update.Should().ContainSingle().Which.Event.Should().BeSameAs(hostedEvent);
    }

    [Test]
    public void Plan_LeavesAnUnchangedEventAlone()
    {
        var plan = DiscordEventImport.Plan(new[] { Listed("1") }, new[] { Imported("1") }, NoDismissals);

        plan.Create.Should().BeEmpty();
        plan.Update.Should().BeEmpty();
        plan.Remove.Should().BeEmpty();
    }

    [Test]
    public void Plan_LeavesAStartedEventToTheGame()
    {
        var running = Imported("1", HostedEventStatus.Running);

        var changed = DiscordEventImport.Plan(new[] { Listed("1", Start.AddDays(7)) }, new[] { running }, NoDismissals);
        var gone = DiscordEventImport.Plan(new List<DiscordScheduledEvent>(), new[] { running }, NoDismissals);

        changed.Update.Should().BeEmpty();
        changed.Create.Should().BeEmpty();
        gone.Remove.Should().BeEmpty();
    }

    [Test]
    public void Plan_RemovesAnUnstartedEventDiscordNoLongerLists()
    {
        var hostedEvent = Imported("1");
        var madeInGame = new HostedEvent { Name = "Made in game" };

        var plan = DiscordEventImport.Plan(new List<DiscordScheduledEvent>(), new[] { hostedEvent, madeInGame }, NoDismissals);

        plan.Remove.Should().ContainSingle().Which.Should().BeSameAs(hostedEvent);
    }

    [Test]
    public void Plan_KeepsADismissedOccurrenceOffTheList()
    {
        var dismissals = new Dictionary<string, DateTime> { ["1"] = Start };

        var plan = DiscordEventImport.Plan(new[] { Listed("1") }, new List<HostedEvent>(), dismissals);

        plan.Create.Should().BeEmpty();
        plan.DropDismissals.Should().BeEmpty();
    }

    [Test]
    public void Plan_ListsTheNextOccurrenceOfADismissedRecurringEvent()
    {
        var dismissals = new Dictionary<string, DateTime> { ["1"] = Start };

        var plan = DiscordEventImport.Plan(new[] { Listed("1", Start.AddDays(7)) }, new List<HostedEvent>(), dismissals);

        plan.Create.Should().ContainSingle();
        plan.DropDismissals.Should().Equal("1");
    }

    [Test]
    public void Plan_ForgetsADismissalOnceDiscordDropsTheEvent()
    {
        var dismissals = new Dictionary<string, DateTime> { ["1"] = Start };

        var plan = DiscordEventImport.Plan(new List<DiscordScheduledEvent>(), new List<HostedEvent>(), dismissals);

        plan.DropDismissals.Should().Equal("1");
    }

    [Test]
    public void GetOccurrenceStart_KeepsDiscordsTimeAfterADMEditsAStartedEvent()
    {
        var running = Imported("1", HostedEventStatus.Running);
        running.ScheduledStartUtc = Start.AddHours(3);

        var occurrence = DiscordEventImport.GetOccurrenceStart(running);
        var plan = DiscordEventImport.Plan(new[] { Listed("1") }, new List<HostedEvent>(), new Dictionary<string, DateTime> { ["1"] = occurrence });

        occurrence.Should().Be(Start);
        plan.Create.Should().BeEmpty();
    }

    [Test]
    public void GetOccurrenceStart_UsesTheEventTimeForARecordWithoutADiscordTime()
    {
        var hostedEvent = new HostedEvent { DiscordEventId = "1", ScheduledStartUtc = Start };

        DiscordEventImport.GetOccurrenceStart(hostedEvent).Should().Be(Start);
    }

    [Test]
    public void IsSyncedFromDiscord_OnlyForAnImportedEventNoDMHasStarted()
    {
        DiscordEventImport.IsSyncedFromDiscord(Imported("1")).Should().BeTrue();
        DiscordEventImport.IsSyncedFromDiscord(Imported("1", HostedEventStatus.Running)).Should().BeFalse();
        DiscordEventImport.IsSyncedFromDiscord(new HostedEvent()).Should().BeFalse();
        DiscordEventImport.IsSyncedFromDiscord(null).Should().BeFalse();
    }
}
