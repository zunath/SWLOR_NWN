using System.Collections;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Gameplay;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// The two <c>mod_load</c> handlers that split <c>ServerTasks.ProcessBootUp</c> (P4-8): <c>ProcessBootUp</c> sets the play
/// options and the bans in the engine and sends the boot-complete notification (boot only), then
/// <c>StartRestartReminders</c> starts the reminder state and its one-minute timer (a hot reload reruns it). Together they
/// make the calls the single handler made; only the notification now goes out before the reminders start, two steps
/// with nothing between them. The engine plugin, the database, the reminder state and the job queue are doubles; the
/// scheduler is the game's static one, cleaned up around each test.
/// </summary>
[TestFixture]
[NonParallelizable]
public class ServerTasksBootTests
{
    private const string WebhookUrl = "https://example.invalid/webhook";

    private FakeAdministrationPlugin _administration = null!;
    private ScriptedProxy _reminders = null!;
    private ScriptedProxy _jobs = null!;
    private FacadeContainer _container = null!;
    private readonly List<IDisposable> _scopes = new();
    private List<object> _scheduledBefore = null!;
    private List<string> _events = null!;

    [SetUp]
    public void SetUp()
    {
        _scheduledBefore = ScheduledItems().Cast<object>().ToList();
        _events = new List<string>();

        var db = new InMemoryDBService()
            .With(new PlayerBan { CDKey = "AAAA1111", Reason = "spam" })
            .With(new PlayerBan { CDKey = "BBBB2222", Reason = "cheating" });
        var serverTask = ScriptedProxy.Create<IServerTaskService>(out _reminders);
        _reminders.On(nameof(IServerTaskService.StartRestartReminders), _ => { _events.Add($"reminders after {AdministrationCalls()}"); return null; })
            .On("get_" + nameof(IServerTaskService.ServerNotificationWebhookUrl), _ => WebhookUrl);
        var backgroundJob = ScriptedProxy.Create<IBackgroundJobService>(out _jobs);
        _jobs.On(nameof(IBackgroundJobService.EnqueueDiscordWebhook), arguments =>
        {
            _events.Add($"webhook '{arguments[2]}' after {AdministrationCalls()}");
            return Task.FromResult(true);
        });

        _container = new FacadeContainer(services =>
        {
            services.Replace(ServiceDescriptor.Singleton<IDBService>(db));
            services.Replace(ServiceDescriptor.Singleton(serverTask));
            services.Replace(ServiceDescriptor.Singleton(backgroundJob));
        });
        _scopes.Add(_container.Install());

        _administration = new FakeAdministrationPlugin();
        _scopes.Add(AdministrationPluginRuntime.Override(_administration));
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var scope in Enumerable.Reverse(_scopes))
            scope.Dispose();
        _scopes.Clear();
        foreach (var item in NewScheduledItems())
            ((IDisposable)item).Dispose();
        _container.Dispose();
    }

    [Test]
    public void ProcessBootUp_then_StartRestartReminders_make_the_calls_of_the_single_handler()
    {
        var before = DateTime.UtcNow;

        ServerTasks.ProcessBootUp();
        ServerTasks.StartRestartReminders();

        _administration.Calls.Select(call => $"{call.Member} {string.Join(",", call.Arguments)}").Should().Equal(
            $"SetPlayOption {AdministrationOption.ExamineChallengeRating},False",
            $"SetPlayOption {AdministrationOption.UseMaxHitpoints},True",
            "AddBannedCDKey AAAA1111",
            "AddBannedCDKey BBBB2222");
        _events.Should().Equal(
            "webhook 'Server boot process is complete. Server is fully online and available for play.' after 4",
            "reminders after 4");
        var bootNow = (DateTime)_reminders.Calls.Single(call => call.Member == nameof(IServerTaskService.StartRestartReminders)).Arguments[0]!;
        bootNow.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTime.UtcNow);
        NewScheduledItems().Should().ContainSingle("the one-minute reminder check is scheduled once");
    }

    [Test]
    public void ProcessBootUp_alone_starts_no_reminder_and_no_timer()
    {
        ServerTasks.ProcessBootUp();

        _events.Should().Equal("webhook 'Server boot process is complete. Server is fully online and available for play.' after 4");
        _reminders.Calls.Should().NotContain(call => call.Member == nameof(IServerTaskService.StartRestartReminders));
        NewScheduledItems().Should().BeEmpty();
    }

    [Test]
    public void StartRestartReminders_alone_only_starts_the_reminders()
    {
        ServerTasks.StartRestartReminders();

        _events.Should().Equal("reminders after 0");
        _administration.Calls.Should().BeEmpty("a hot reload reruns this part, so it must not set engine options or bans again");
        _jobs.Calls.Should().BeEmpty("the boot-complete notification is sent once, at boot");
        _reminders.Calls.Should().NotContain(call => call.Member == "get_" + nameof(IServerTaskService.ServerNotificationWebhookUrl));
        NewScheduledItems().Should().ContainSingle();
    }

    private int AdministrationCalls() => _administration.Calls.Count;

    private static IList ScheduledItems() =>
        (IList)typeof(Scheduler).GetField("_scheduledItems", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;

    private List<object> NewScheduledItems() => ScheduledItems().Cast<object>().Except(_scheduledBefore).ToList();
}
