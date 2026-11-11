using System.Globalization;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Core;

/// <summary>
/// Unit tests of the background job queue: the database fake records the stream entries the service adds, so the
/// entry fields, the payload JSON, the stream limits and the failure results can be asserted.
/// </summary>
[TestFixture]
public class BackgroundJobServiceTests
{
    private InMemoryDBService _db = null!;
    private RecordingBackgroundJobService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new InMemoryDBService();
        _service = new RecordingBackgroundJobService(_db);
    }

    private static string Field(StreamAddCall call, string name) =>
        call.Entries.Single(entry => entry.Name == name).Value.ToString();

    [Test]
    public void The_stream_name_and_limit_are_the_ones_the_worker_reads()
    {
        BackgroundJobService.StreamName.Should().Be("swlor:background-jobs");
        BackgroundJobService.MaxStreamLength.Should().Be(10000);
    }

    [Test]
    public async Task A_github_issue_is_added_to_the_stream_with_its_payload()
    {
        var before = DateTime.UtcNow;

        var queued = await _service.EnqueueGitHubIssue("zunath/SWLOR_NWN", "Crash on login", "Steps to reproduce");

        queued.Should().BeTrue();
        var call = _db.StreamAdds.Should().ContainSingle().Subject;
        call.Key.Should().Be("swlor:background-jobs");
        call.MaxLength.Should().Be(10000);
        call.UseApproximateMaxLength.Should().BeTrue();
        call.Entries.Select(entry => entry.Name.ToString()).Should().Equal("type", "payload", "createdUtc");
        Field(call, "type").Should().Be("GitHubIssue");
        JObject.Parse(Field(call, "payload")).Should().BeEquivalentTo(JObject.FromObject(new
        {
            repository = "zunath/SWLOR_NWN",
            title = "Crash on login",
            body = "Steps to reproduce"
        }));
        var created = DateTime.Parse(Field(call, "createdUtc"), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        created.Kind.Should().Be(DateTimeKind.Utc);
        created.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTime.UtcNow);
        _service.Logged.Should().BeEmpty();
    }

    [Test]
    public async Task A_discord_webhook_is_added_to_the_stream_with_its_payload()
    {
        var queued = await _service.EnqueueDiscordWebhook("https://discord.example/hook", "Zunath", "Server is up", 3447003,
            "Status", "thread-9", true, "Lifecycle");

        queued.Should().BeTrue();
        var call = _db.StreamAdds.Should().ContainSingle().Subject;
        Field(call, "type").Should().Be("DiscordWebhook");
        JObject.Parse(Field(call, "payload")).Should().BeEquivalentTo(JObject.FromObject(new
        {
            webhookUrl = "https://discord.example/hook",
            authorName = "Zunath",
            title = "Status",
            description = "Server is up",
            color = 3447003,
            threadId = "thread-9",
            createThread = true,
            threadName = "Lifecycle"
        }));
    }

    [Test]
    public async Task A_discord_webhook_without_the_optional_fields_sends_them_empty()
    {
        await _service.EnqueueDiscordWebhook("https://discord.example/hook", "Zunath", "Hello", 1);

        var payload = JObject.Parse(Field(_db.StreamAdds.Single(), "payload"));
        payload["title"]!.Value<string>().Should().BeEmpty();
        payload["threadId"]!.Value<string>().Should().BeEmpty();
        payload["createThread"]!.Value<bool>().Should().BeFalse();
        payload["threadName"]!.Value<string>().Should().BeEmpty();
    }

    [Test]
    public async Task A_failed_stream_write_logs_the_job_and_reports_false()
    {
        _db.StreamAddFailure = new InvalidOperationException("redis is down");

        var queued = await _service.EnqueueDiscordWebhook("https://discord.example/hook", "Zunath", "Hello", 1,
            threadId: "thread-9", createThread: true);

        queued.Should().BeFalse();
        var logged = _service.Logged.Should().ContainSingle().Subject;
        logged.Group.Should().Be(LogGroup.Error);
        logged.Details.Should().StartWith("Failed to enqueue background job. Type='DiscordWebhook'. Context='threadId='thread-9', createThread='True''. ")
            .And.Contain("redis is down");
    }

    [Test]
    public async Task A_failed_github_issue_logs_only_the_repository()
    {
        _db.StreamAddFailure = new InvalidOperationException("redis is down");

        var queued = await _service.EnqueueGitHubIssue("zunath/SWLOR_NWN", "Title that stays private", "Body that stays private");

        queued.Should().BeFalse();
        var logged = _service.Logged.Should().ContainSingle().Subject;
        logged.Details.Should().StartWith("Failed to enqueue background job. Type='GitHubIssue'. Context='repository='zunath/SWLOR_NWN''. ")
            .And.NotContain("stays private");
    }

    [Test]
    public async Task The_log_goes_to_the_static_log_by_default()
    {
        _db.StreamAddFailure = new InvalidOperationException("redis is down");
        var service = new BackgroundJobService(_db);

        // Nothing is registered with the static log in a unit test, so the write is a no-op rather than a failure.
        var queued = await service.EnqueueGitHubIssue("zunath/SWLOR_NWN", "Title", "Body");

        queued.Should().BeFalse();
    }

    private sealed class RecordingBackgroundJobService : BackgroundJobService
    {
        public RecordingBackgroundJobService(IDBService db)
            : base(db)
        {
        }

        public List<(LogGroup Group, string Details)> Logged { get; } = new();

        protected override void WriteLog(LogGroup group, string details, bool printToConsole = false) =>
            Logged.Add((group, details));
    }
}
