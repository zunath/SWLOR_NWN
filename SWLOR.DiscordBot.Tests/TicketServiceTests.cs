using Discord;
using Microsoft.Extensions.Logging.Abstractions;
using SWLOR.DiscordBot.Hosting;
using NUnit.Framework;
using SWLOR.DiscordBot.Configuration;
using SWLOR.DiscordBot.Core;
using SWLOR.DiscordBot.Discord;
using SWLOR.DiscordBot.Persistence;

namespace SWLOR.DiscordBot.Tests;

[TestFixture]
public sealed class TicketServiceTests
{
    private const ulong SupportRole = 900;
    private const ulong ChannelOne = 10001;
    private static readonly Actor Support = new(9001, [SupportRole]);
    private BotConfiguration _configuration = null!;
    private MemoryTicketStore _store = null!;
    private FakeDiscordTickets _discord = null!;
    private FakeArchive _archive = null!;
    private MutableTimeProvider _clock = null!;
    private TicketService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _configuration = new BotConfiguration
        {
            GuildId = 1,
            Tickets = new TicketOptions
            {
                Enabled = true,
                SupportRoleIds = [SupportRole],
                MemberLimit = 3,
                GuildLimit = 5,
                CleanupDelay = TimeSpan.FromDays(7),
                ArchiveRetentionDays = 90,
                Panels =
                [
                    new TicketPanelOptions { Id = "first", OpenLimit = 2 },
                    new TicketPanelOptions { Id = "second", OpenLimit = 2 }
                ]
            }
        };
        _store = new MemoryTicketStore();
        _discord = new FakeDiscordTickets();
        _discord.CurrentActors[Support.UserId] = Support;
        _archive = new FakeArchive();
        _clock = new MutableTimeProvider(new DateTimeOffset(2026, 10, 3, 12, 0, 0, TimeSpan.Zero));
        _service = new TicketService(_configuration, _store, _discord, _archive, _clock);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task NonSupportCannotCloseExportRenameOrReopenAnotherMembersTicket(bool intakeEnabled)
    {
        var requester = new Actor(1, []);
        var opened = await Open("first", requester, "unauthorized-check");
        _configuration.Tickets.Enabled = intakeEnabled;
        var stranger = new Actor(2, []);

        var close = await _service.CloseAsync(ChannelOne, stranger);
        var export = await _service.ExportAsync(ChannelOne, stranger);
        var rename = await _service.RenameAsync(ChannelOne, stranger, "hacked name");
        var hold = await _service.SetHoldAsync(ChannelOne, stranger, true);
        var release = await _service.SetHoldAsync(ChannelOne, stranger, false);
        var closeBySupport = await _service.CloseAsync(ChannelOne, Support);
        var reopen = await _service.ReopenAsync(ChannelOne, stranger);

        Assert.Multiple(() =>
        {
            Assert.That(opened.Success, Is.True);
            Assert.That(close.Success, Is.False);
            Assert.That(export.Success, Is.False);
            Assert.That(rename.Success, Is.False);
            Assert.That(hold.Success, Is.False);
            Assert.That(release.Success, Is.False);
            Assert.That(reopen.Success, Is.False);
            Assert.That(closeBySupport.Success, Is.True);
            Assert.That(_discord.CloseCalls, Is.EqualTo(1));
            Assert.That(_discord.RenameCalls, Is.Zero);
            Assert.That(_archive.ExportCalls, Is.Zero);
            Assert.That(_discord.OpenCalls, Is.EqualTo(1), "unauthorized reopen must not open the channel");
        });
    }

    [TestCase("close", false, false)]
    [TestCase("close", false, true)]
    [TestCase("close", true, false)]
    [TestCase("close", true, true)]
    [TestCase("rename", false, false)]
    [TestCase("rename", false, true)]
    [TestCase("rename", true, false)]
    [TestCase("rename", true, true)]
    [TestCase("reopen", false, false)]
    [TestCase("reopen", false, true)]
    [TestCase("reopen", true, false)]
    [TestCase("reopen", true, true)]
    [TestCase("hold", false, false)]
    [TestCase("hold", false, true)]
    [TestCase("hold", true, false)]
    [TestCase("hold", true, true)]
    [TestCase("release", false, false)]
    [TestCase("release", false, true)]
    [TestCase("release", true, false)]
    [TestCase("release", true, true)]
    public async Task QueuedMutationRechecksRevokedSupportOrOwnerAccess(string operation, bool owner, bool intakeEnabled)
    {
        await Open("first", new Actor(1, []), "queued-authorization");
        if (operation == "reopen") await _service.CloseAsync(ChannelOne, Support);
        if (operation == "release") await _service.SetHoldAsync(ChannelOne, Support, true);
        _configuration.Tickets.Enabled = intakeEnabled;
        var actor = owner ? new Actor(Support.UserId, [], IsGuildOwner: true) : Support;
        _discord.CurrentActors[actor.UserId] = actor;
        var before = _store.Tickets.Single();
        var auditsBefore = _store.Audits.Count;
        var blockingSession = await _store.LockAsync(default);
        Task<TicketResult> queued;
        try
        {
            queued = Mutate(operation, actor);
            Assert.That(queued.IsCompleted, Is.False, "The command must queue behind another ticket operation.");
            _discord.CurrentActors[actor.UserId] = new Actor(actor.UserId, []);
        }
        finally { await blockingSession.DisposeAsync(); }
        var result = await queued.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(_store.Tickets.Single(), Is.EqualTo(before));
            Assert.That(_store.Audits.Count, Is.EqualTo(auditsBefore), "Denied access must not persist any mutation.");
        });
        _discord.CurrentActors[actor.UserId] = actor;
        Assert.That((await Mutate(operation, actor)).Success, Is.True, "Denied access must release the shared lock.");
    }

    [TestCase("close")]
    [TestCase("rename")]
    [TestCase("reopen")]
    [TestCase("hold")]
    [TestCase("release")]
    public async Task QueuedMutationFailsClosedWhenStaffMemberLeavesGuild(string operation)
    {
        await Open("first", new Actor(1, []), "queued-departure");
        if (operation == "reopen") await _service.CloseAsync(ChannelOne, Support);
        if (operation == "release") await _service.SetHoldAsync(ChannelOne, Support, true);
        var before = _store.Tickets.Single();
        var blockingSession = await _store.LockAsync(default);
        Task<TicketResult> queued;
        try
        {
            queued = Mutate(operation, Support);
            Assert.That(queued.IsCompleted, Is.False);
            _discord.MissingMembers.Add(Support.UserId);
        }
        finally { await blockingSession.DisposeAsync(); }
        Assert.ThrowsAsync<InvalidOperationException>(async () => await queued.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.That(_store.Tickets.Single(), Is.EqualTo(before));
        _discord.MissingMembers.Clear();
        Assert.That((await Mutate(operation, Support)).Success, Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task QueuedMutationUsesNewlyGrantedCurrentSupportOrOwnerAccess(bool owner)
    {
        await Open("first", new Actor(1, []), "queued-grant");
        var stale = new Actor(Support.UserId, []);
        _discord.CurrentActors[stale.UserId] = stale;
        var blockingSession = await _store.LockAsync(default);
        Task<TicketResult> queued;
        try
        {
            queued = _service.RenameAsync(ChannelOne, stale, "current-access");
            Assert.That(queued.IsCompleted, Is.False);
            _discord.CurrentActors[stale.UserId] = owner ? stale with { IsGuildOwner = true } : Support;
        }
        finally { await blockingSession.DisposeAsync(); }
        Assert.That((await queued.WaitAsync(TimeSpan.FromSeconds(5))).Success, Is.True);
        Assert.That(_discord.RenameCalls, Is.EqualTo(1));
    }

    [Test]
    public async Task RepeatedMaintenanceFailureAuditsOnlyTheFirstDurableErrorAndAuditsAgainAfterRecovery()
    {
        await Open("first", new Actor(1, []), "maintenance-audit");
        await _service.CloseAsync(ChannelOne, Support);
        _discord.BeforeExistsAsync = _ => Task.FromException(new InvalidOperationException("Persistent lookup failure."));
        for (var attempt = 0; attempt < 3; attempt++) await _service.MaintainAsync();
        Assert.That(_store.Audits.Count(action => action == "maintenance-failed"), Is.EqualTo(1));
        Assert.That(_store.Tickets.Single().LastError, Is.Not.Null);
        _discord.BeforeExistsAsync = null;
        Assert.That((await _service.ReopenAsync(ChannelOne, Support)).Success, Is.True);
        Assert.That(_store.Tickets.Single().LastError, Is.Null);
        _discord.BeforeExistsAsync = _ => Task.FromException(new InvalidOperationException("New failure after recovery."));
        await _service.MaintainAsync();
        await _service.MaintainAsync();
        Assert.That(_store.Audits.Count(action => action == "maintenance-failed"), Is.EqualTo(2));
    }

    [Test]
    public async Task RepeatedFailedCleanupKeepsOneOwnershipAndFailureAuditUntilAnActualStateChange()
    {
        await Open("first", new Actor(1, []), "cleanup-audit");
        await _service.CloseAsync(ChannelOne, Support);
        _clock.Advance(TimeSpan.FromDays(8));
        _archive.FailExports = true;
        await _service.MaintainAsync();
        var auditCount = _store.Audits.Count;
        await _service.MaintainAsync();
        await _service.MaintainAsync();
        Assert.Multiple(() =>
        {
            Assert.That(_archive.ExportCalls, Is.EqualTo(3), "Cleanup must keep retrying the operation.");
            Assert.That(_store.Audits.Count, Is.EqualTo(auditCount), "Unchanged retries must not grow the durable audit.");
            Assert.That(_store.Audits.Count(action => action == "archive-pending"), Is.EqualTo(1));
            Assert.That(_store.Audits.Count(action => action == "maintenance-failed"), Is.EqualTo(1));
            Assert.That(_store.Tickets.Single().State, Is.EqualTo(TicketState.Deleting));
        });
        _archive.FailExports = false;
        await _service.MaintainAsync();
        Assert.That(_store.Tickets.Single().State, Is.EqualTo(TicketState.Deleted));
        Assert.That(_store.Tickets.Single().LastError, Is.Null);
    }

    [TestCase("member", false)]
    [TestCase("member", true)]
    [TestCase("panel", false)]
    [TestCase("panel", true)]
    [TestCase("guild", false)]
    [TestCase("guild", true)]
    public async Task QueuedOpenUsesCurrentBypassRoleForEveryLimit(string scope, bool granted)
    {
        await Open("first", new Actor(1, []), "current-limit-existing");
        _configuration.Tickets.MemberLimit = scope == "member" ? 1 : 10;
        _configuration.Tickets.Panels[0].OpenLimit = scope == "panel" ? 1 : 10;
        _configuration.Tickets.GuildLimit = scope == "guild" ? 1 : 10;
        _configuration.Tickets.BypassRoleIds = [901];
        SetBypassScope(scope, true);
        var captured = new Actor(1, granted ? [] : [901]);
        _discord.CurrentActors[1] = captured;
        var blockingSession = await _store.LockAsync(default);
        Task<TicketResult> queued;
        try
        {
            queued = _service.OpenAsync("first", captured, "current-limit-queued");
            Assert.That(queued.IsCompleted, Is.False);
            _discord.CurrentActors[1] = new Actor(1, granted ? [901] : []);
        }
        finally { await blockingSession.DisposeAsync(); }
        Assert.That((await queued.WaitAsync(TimeSpan.FromSeconds(5))).Success, Is.EqualTo(granted));
        Assert.That(_discord.CreateCalls, Is.EqualTo(granted ? 2 : 1));
        Assert.That(_store.Tickets, Has.Count.EqualTo(granted ? 2 : 1));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task QueuedOpenFailsClosedForDepartedMemberIncludingPendingCreation(bool resume)
    {
        var requester = new Actor(1, []);
        if (resume)
        {
            _discord.FailNextOpen = true;
            Assert.That((await Open("first", requester, "queued-departed")).Success, Is.False);
        }
        var ticketCount = _store.Tickets.Count;
        var createCalls = _discord.CreateCalls;
        var openCalls = _discord.OpenCalls;
        var blockingSession = await _store.LockAsync(default);
        Task<TicketResult> queued;
        try
        {
            queued = _service.OpenAsync("first", requester, "queued-departed");
            Assert.That(queued.IsCompleted, Is.False);
            _discord.MissingMembers.Add(requester.UserId);
        }
        finally { await blockingSession.DisposeAsync(); }
        Assert.ThrowsAsync<InvalidOperationException>(async () => await queued.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Multiple(() =>
        {
            Assert.That(_store.Tickets.Count, Is.EqualTo(ticketCount));
            Assert.That(_discord.CreateCalls, Is.EqualTo(createCalls));
            Assert.That(_discord.OpenCalls, Is.EqualTo(openCalls));
        });
        _discord.MissingMembers.Clear();
        Assert.That((await _service.OpenAsync("first", requester, "queued-departed")).Success, Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ArchiveExpirationDoesNotBlockOtherTicketsAndGuardsItsOwnRetention(bool maintenance)
    {
        await Open("first", new Actor(1, []), "long-expiration");
        await Open("second", new Actor(2, []), "unrelated-expiration");
        var expired = _store.Tickets.Single(ticket => ticket.ChannelId == ChannelOne) with
        {
            State = TicketState.Closed, ArchivePath = "/archives/expired", ArchiveComplete = true,
            ArchiveExpiresAt = _clock.GetUtcNow().AddSeconds(-1)
        };
        _store.Replace(expired);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _archive.BeforeDeleteArchiveAsync = async (_, _, token) =>
        {
            started.TrySetResult();
            await release.Task.WaitAsync(token);
        };
        var expiration = maintenance ? _service.MaintainAsync() : _service.ExpireArchivesAsync();
        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.That((await _service.RenameAsync(ChannelOne + 1, Support, "unrelated")).Success, Is.True);
            Assert.That((await _service.CloseAsync(ChannelOne + 1, Support).WaitAsync(TimeSpan.FromSeconds(2))).Success, Is.True);
            Assert.That((await _service.ReopenAsync(ChannelOne + 1, Support).WaitAsync(TimeSpan.FromSeconds(2))).Success, Is.True);
            Assert.That((await Open("second", new Actor(3, []), "open-during-expiration").WaitAsync(TimeSpan.FromSeconds(2))).Success, Is.True);
            Assert.That((await _service.SetHoldAsync(ChannelOne, Support, true).WaitAsync(TimeSpan.FromSeconds(2))).Success, Is.False);
            Assert.That((await _service.ExportAsync(ChannelOne, Support, useSavedArchive: true).WaitAsync(TimeSpan.FromSeconds(2))).Success, Is.False);
            Assert.That(_store.Tickets.Single(ticket => ticket.Id == expired.Id).ArchivePath, Is.EqualTo(expired.ArchivePath));
            await _service.ExpireArchivesAsync().WaitAsync(TimeSpan.FromSeconds(2));
            release.TrySetResult();
            await expiration.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.That(_archive.DeletedPaths, Is.EqualTo(new[] { expired.ArchivePath }));
            Assert.That(_store.Tickets.Single(ticket => ticket.Id == expired.Id).ArchivePath, Is.Null);
        }
        finally { release.TrySetResult(); await expiration; }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ArchiveExpirationReleasesGuardAfterCancellationOrFailureAndRetainsOwnership(bool cancelled)
    {
        await Open("first", new Actor(1, []), "expiration-failure");
        var expired = _store.Tickets.Single() with
        {
            State = TicketState.Closed, ArchivePath = "/archives/expired", ArchiveComplete = true,
            ArchiveExpiresAt = _clock.GetUtcNow().AddSeconds(-1)
        };
        _store.Replace(expired);
        using var cancellation = new CancellationTokenSource();
        _archive.BeforeDeleteArchiveAsync = (_, _, token) =>
        {
            if (!cancelled) return Task.FromException(new IOException("Simulated deletion failure."));
            cancellation.Cancel();
            token.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        };
        if (cancelled) Assert.CatchAsync<OperationCanceledException>(() => _service.ExpireArchivesAsync(cancellation.Token));
        else await CompleteExpirationRetriesAsync(_service.ExpireArchivesAsync());
        Assert.That(_store.Tickets.Single().ArchivePath, Is.EqualTo(expired.ArchivePath));
        Assert.That((await _service.SetHoldAsync(ChannelOne, Support, true)).Success, Is.True);
        _archive.BeforeDeleteArchiveAsync = null;
        await _service.ExpireArchivesAsync();
        Assert.That(_archive.DeletedPaths, Is.Empty, "A hold set after failure prevents the next deletion.");
        await _service.SetHoldAsync(ChannelOne, Support, false);
        await _service.ExpireArchivesAsync();
        Assert.That(_archive.DeletedPaths, Is.EqualTo(new[] { expired.ArchivePath }));
        Assert.That(_store.Tickets.Single().ArchivePath, Is.Null);
    }

    [Test]
    public async Task ArchiveExpirationKeepsDurableOwnershipIfCompletionSaveFailsAndRetriesAfterRestart()
    {
        await Open("first", new Actor(1, []), "expiration-save-failure");
        var expired = _store.Tickets.Single() with
        {
            State = TicketState.Closed, ArchivePath = "/archives/expired", ArchiveComplete = true,
            ArchiveExpiresAt = _clock.GetUtcNow().AddSeconds(-1)
        };
        _store.Replace(expired);
        _store.FailNextSaveAction = "archive-expired";
        using var stopping = new CancellationTokenSource();
        var interrupted = _service.ExpireArchivesAsync(stopping.Token);
        await WaitForExpirationBackoffAsync(interrupted);
        Assert.That(_store.Tickets.Single().ArchivePath, Is.EqualTo(expired.ArchivePath));
        stopping.Cancel();
        Assert.CatchAsync<OperationCanceledException>(() => interrupted);
        var restarted = new TicketService(_configuration, _store, _discord, _archive, _clock);
        await restarted.ExpireArchivesAsync();
        Assert.That(_store.Tickets.Single().ArchivePath, Is.Null);
        Assert.That(_store.Audits.Count(action => action == "archive-expired"), Is.EqualTo(1));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ArchiveExpirationBoundsInactivityWhileAllowingLongProgressingDeletion(bool progressing)
    {
        await Open("first", new Actor(1, []), "expiration-progress");
        await Open("second", new Actor(2, []), "expiration-later");
        foreach (var ticket in _store.Tickets)
            _store.Replace(ticket with
            {
                State = TicketState.Closed, ArchivePath = $"/archives/expired-{ticket.ChannelId}", ArchiveComplete = true,
                ArchiveExpiresAt = _clock.GetUtcNow().AddSeconds(-1)
            });
        _archive.BeforeDeleteArchiveAsync = async (path, progress, token) =>
        {
            if (!path.EndsWith(ChannelOne.ToString(), StringComparison.Ordinal)) return;
            for (var step = 0; step < 12; step++)
            {
                _clock.Advance(TimeSpan.FromMinutes(1));
                if (progressing) progress();
                await Task.Yield();
                token.ThrowIfCancellationRequested();
            }
        };
        using var worker = new BotWorker(_configuration, new BotSecrets("", ""), null!, null!, _store,
            _service, null!, null!, null!, null!, _clock, null!, NullLogger<BotWorker>.Instance);
        await CompleteExpirationRetriesAsync(worker.RetryMaintenanceAsync("Archive expiration", _service.ExpireArchivesAsync, CancellationToken.None))
            .WaitAsync(TimeSpan.FromSeconds(2));
        var first = _store.Tickets.Single(ticket => ticket.ChannelId == ChannelOne);
        var later = _store.Tickets.Single(ticket => ticket.ChannelId == ChannelOne + 1);
        Assert.That(_archive.DeletedPaths.Count, Is.EqualTo(progressing ? 2 : 1));
        Assert.That(first.ArchivePath, progressing ? Is.Null : Is.Not.Null);
        Assert.That(first.LastError, progressing ? Is.Null : Is.Not.Null);
        Assert.That(later.ArchivePath, Is.Null, "A stalled earlier item must not starve later expiration.");
        Assert.That(later.LastError, Is.Null);
        if (!progressing)
        {
            Assert.That((await _service.SetHoldAsync(ChannelOne, Support, true)).Success, Is.True);
            _archive.BeforeDeleteArchiveAsync = null;
            await _service.SetHoldAsync(ChannelOne, Support, false);
            await _service.ExpireArchivesAsync();
            Assert.That(_store.Tickets.Single(ticket => ticket.ChannelId == ChannelOne).ArchivePath, Is.Null);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ExpirationRetriesFailedIdsAfterLaterEntriesFinish(bool stalled)
    {
        await Open("first", new Actor(1, []), "retry-first");
        await Open("second", new Actor(2, []), "retry-later");
        foreach (var ticket in _store.Tickets)
            _store.Replace(ticket with { State = TicketState.Closed, ArchivePath = $"/archives/expired-{ticket.ChannelId}",
                ArchiveComplete = true, ArchiveExpiresAt = _clock.GetUtcNow().AddSeconds(-1) });
        var visits = new List<string>();
        _archive.BeforeDeleteArchiveAsync = (path, progress, token) =>
        {
            visits.Add(path);
            if (path.EndsWith(ChannelOne.ToString(), StringComparison.Ordinal) && visits.Count == 1)
            {
                if (stalled)
                {
                    _clock.Advance(TimeSpan.FromMinutes(4));
                    token.ThrowIfCancellationRequested();
                }
                throw new IOException("Transient archive failure.");
            }
            progress();
            return Task.CompletedTask;
        };
        var before = _clock.GetUtcNow();
        _store.TicketEnumerationCount = 0;
        using var worker = new BotWorker(_configuration, new BotSecrets("", ""), null!, null!, _store,
            _service, null!, null!, null!, null!, _clock, null!, NullLogger<BotWorker>.Instance);
        var expiration = worker.RetryMaintenanceAsync("Archive expiration", _service.ExpireArchivesAsync, CancellationToken.None);
        await WaitForExpirationBackoffAsync(expiration);
        Assert.That(visits, Is.EqualTo(new[] { $"/archives/expired-{ChannelOne}", $"/archives/expired-{ChannelOne + 1}" }));
        Assert.That(_store.Tickets.Single(t => t.ChannelId == ChannelOne).ArchivePath, Is.Not.Null);
        Assert.That(_store.Tickets.Single(t => t.ChannelId == ChannelOne + 1).ArchivePath, Is.Null);
        _clock.Advance(TimeSpan.FromSeconds(4));
        Assert.That(visits, Has.Count.EqualTo(2), "The retry honors its backoff.");
        _clock.Advance(TimeSpan.FromSeconds(1));
        await expiration.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.That(visits, Is.EqualTo(new[] { $"/archives/expired-{ChannelOne}", $"/archives/expired-{ChannelOne + 1}", $"/archives/expired-{ChannelOne}" }));
        Assert.That(_store.Tickets.All(t => t.ArchivePath is null && t.LastError is null), Is.True);
        Assert.That(_store.Audits.Count(a => a == "maintenance-failed"), Is.EqualTo(1));
        Assert.That(_store.Audits.Count(a => a == "archive-expired"), Is.EqualTo(2));
        Assert.That(_store.TicketEnumerationCount, Is.EqualTo(1), "Retries use failed IDs rather than rereading successful entries.");
        Assert.That(_clock.GetUtcNow() - before, Is.EqualTo(TimeSpan.FromSeconds(5) + (stalled ? TimeSpan.FromMinutes(4) : TimeSpan.Zero)));
    }

    [Test]
    public async Task ExpirationPermanentFailureStopsAfterThreeAttemptsAndCoalescesAudit()
    {
        await Open("first", new Actor(1, []), "permanent-expiration");
        var expired = _store.Tickets.Single() with { State = TicketState.Closed, ArchivePath = "/archives/permanent",
            ArchiveComplete = true, ArchiveExpiresAt = _clock.GetUtcNow().AddSeconds(-1) };
        _store.Replace(expired);
        var attempts = 0;
        _archive.BeforeDeleteArchiveAsync = (_, _, _) =>
        {
            attempts++;
            throw new IOException("Persistent archive failure.");
        };
        var before = _clock.GetUtcNow();
        var expiration = _service.ExpireArchivesAsync();
        await WaitForExpirationBackoffAsync(expiration);
        _clock.Advance(TimeSpan.FromSeconds(5));
        await WaitForExpirationBackoffAsync(expiration);
        Assert.That(attempts, Is.EqualTo(2));
        _clock.Advance(TimeSpan.FromSeconds(9));
        Assert.That(attempts, Is.EqualTo(2));
        _clock.Advance(TimeSpan.FromSeconds(1));
        await expiration.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.That(attempts, Is.EqualTo(3));
        Assert.That(_clock.GetUtcNow() - before, Is.EqualTo(TimeSpan.FromSeconds(15)));
        Assert.That(_store.Tickets.Single().ArchivePath, Is.EqualTo(expired.ArchivePath));
        Assert.That(_store.Audits.Count(a => a == "maintenance-failed"), Is.EqualTo(1));
        _archive.BeforeDeleteArchiveAsync = null;
        await _service.ExpireArchivesAsync();
        Assert.That(_store.Tickets.Single().ArchivePath, Is.Null);
        Assert.That(_store.Tickets.Single().LastError, Is.Null);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ExpirationRetryBackoffObservesNewHoldOrShutdown(bool shutdown)
    {
        await Open("first", new Actor(1, []), "expiration-interrupted-retry");
        var expired = _store.Tickets.Single() with { State = TicketState.Closed, ArchivePath = "/archives/interrupted",
            ArchiveComplete = true, ArchiveExpiresAt = _clock.GetUtcNow().AddSeconds(-1) };
        _store.Replace(expired);
        var attempts = 0;
        _archive.BeforeDeleteArchiveAsync = (_, _, _) =>
        {
            attempts++;
            throw new IOException("Archive failure before retry.");
        };
        using var stopping = new CancellationTokenSource();
        var expiration = _service.ExpireArchivesAsync(stopping.Token);
        await WaitForExpirationBackoffAsync(expiration);
        try
        {
            Assert.That((await _service.SetHoldAsync(ChannelOne, Support, true).WaitAsync(TimeSpan.FromSeconds(2))).Success, Is.True);
            if (shutdown)
            {
                stopping.Cancel();
                Assert.CatchAsync<OperationCanceledException>(() => expiration.WaitAsync(TimeSpan.FromSeconds(2)));
            }
            else
            {
                _clock.Advance(TimeSpan.FromSeconds(5));
                await expiration.WaitAsync(TimeSpan.FromSeconds(2));
            }
            Assert.That(attempts, Is.EqualTo(1), "The next attempt must honor shutdown/current hold.");
            Assert.That(_store.Tickets.Single().ArchivePath, Is.EqualTo(expired.ArchivePath));
            Assert.That(_store.Tickets.Single().Hold, Is.True);
            _archive.BeforeDeleteArchiveAsync = null;
            await _service.SetHoldAsync(ChannelOne, Support, false);
            await _service.ExpireArchivesAsync();
            Assert.That(_store.Tickets.Single().ArchivePath, Is.Null);
        }
        finally { stopping.Cancel(); try { await expiration; } catch (OperationCanceledException) { } }
    }

    [Test]
    public async Task ExpirationRetriesInterruptedCompletionSaveInTheSamePass()
    {
        await Open("first", new Actor(1, []), "expiration-save-retry");
        _store.Replace(_store.Tickets.Single() with { State = TicketState.Closed, ArchivePath = "/archives/save-retry",
            ArchiveComplete = true, ArchiveExpiresAt = _clock.GetUtcNow().AddSeconds(-1) });
        _store.FailNextSaveAction = "archive-expired";
        var expiration = _service.ExpireArchivesAsync();
        await WaitForExpirationBackoffAsync(expiration);
        Assert.That(_store.Tickets.Single().ArchivePath, Is.EqualTo("/archives/save-retry"));
        _clock.Advance(TimeSpan.FromSeconds(5));
        await expiration.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.That(_store.Tickets.Single().ArchivePath, Is.Null);
        Assert.That(_archive.DeletedPaths, Has.Count.EqualTo(2), "An already-removed tree can complete its retained ownership on retry.");
        Assert.That(_store.Audits.Count(a => a == "archive-expired"), Is.EqualTo(1));
    }

    private async Task WaitForExpirationBackoffAsync(Task expiration)
    {
        using var safety = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        while (!_clock.HasExpirationBackoff)
        {
            if (expiration.IsCompleted) { await expiration; Assert.Fail("Expected expiration retry backoff."); }
            safety.Token.ThrowIfCancellationRequested();
            await Task.Yield();
        }
    }

    private async Task CompleteExpirationRetriesAsync(Task expiration)
    {
        using var safety = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        while (!expiration.IsCompleted)
        {
            safety.Token.ThrowIfCancellationRequested();
            _clock.AdvanceExpirationBackoff();
            await Task.Yield();
        }
        await expiration;
    }

    private Task<TicketResult> Mutate(string operation, Actor actor) => operation switch
    {
        "close" => _service.CloseAsync(ChannelOne, actor),
        "rename" => _service.RenameAsync(ChannelOne, actor, "renamed-ticket"),
        "reopen" => _service.ReopenAsync(ChannelOne, actor),
        "hold" => _service.SetHoldAsync(ChannelOne, actor, true),
        "release" => _service.SetHoldAsync(ChannelOne, actor, false),
        _ => throw new ArgumentOutOfRangeException(nameof(operation))
    };

    [Test]
    public async Task PerTicketOperationsUseTargetedLookupsAndMaintenanceEnumeratesOnlyOnce()
    {
        var opened = await Open("first", new Actor(1, []), "targeted-lookups");
        Assert.That(opened.Success, Is.True);

        _store.TicketEnumerationCount = 0;
        var closed = await _service.CloseAsync(ChannelOne, Support);
        Assert.That(closed.Success, Is.True);
        Assert.That(_store.TicketEnumerationCount, Is.Zero,
            "A channel mutation should use the channel index rather than enumerate every retained ticket.");

        Assert.That((await Open("first", new Actor(2, []), "another-retained-ticket")).Success, Is.True);
        _store.TicketEnumerationCount = 0;
        await _service.ReconcileRetainedPermissionsAsync();
        Assert.That(_store.TicketEnumerationCount, Is.EqualTo(1),
            "Startup permission reconciliation should enumerate only its batch.");

        _store.TicketEnumerationCount = 0;
        await _service.MaintainAsync();
        Assert.That(_store.TicketEnumerationCount, Is.EqualTo(1),
            "Maintenance should enumerate once for the batch, then fetch each ticket by id.");

        _clock.Advance(TimeSpan.FromDays(8));
        _store.TicketEnumerationCount = 0;
        await _service.MaintainAsync();
        Assert.That(_store.TicketEnumerationCount, Is.EqualTo(1),
            "Cleanup export, final verification, and completion must not scan all retained payloads.");
        Assert.That(_store.Tickets.Single(ticket => ticket.Id == opened.Ticket!.Id).State, Is.EqualTo(TicketState.Deleted));

        _clock.Advance(TimeSpan.FromDays(90));
        _store.TicketEnumerationCount = 0;
        await _service.ExpireArchivesAsync();
        Assert.That(_store.TicketEnumerationCount, Is.EqualTo(1),
            "Local archive expiration should use individual ticket lookups after enumeration.");
    }

    [Test]
    public async Task MemberLimitIsSharedAcrossPanels()
    {
        _configuration.Tickets.MemberLimit = 1;
        var requester = new Actor(1, []);
        var first = await Open("first", requester, "member-first");
        var second = await _service.OpenAsync("second", requester, "member-second");

        Assert.That(first.Success, Is.True);
        Assert.That(second.Success, Is.False);
        Assert.That(_discord.CreateCalls, Is.EqualTo(1));
    }

    [Test]
    public async Task PanelLimitRejectsAnotherMembersTicketWhenPanelIsFull()
    {
        _configuration.Tickets.MemberLimit = 10;
        _configuration.Tickets.Panels.Single(x => x.Id == "first").OpenLimit = 1;
        var first = await Open("first", new Actor(1, []), "panel-first");
        var second = await _service.OpenAsync("first", new Actor(2, []), "panel-second");

        Assert.That(first.Success, Is.True);
        Assert.That(second.Success, Is.False);
        Assert.That(_discord.CreateCalls, Is.EqualTo(1));
    }

    [Test]
    public async Task GuildLimitCountsActiveTicketsAcrossPanels()
    {
        _configuration.Tickets.MemberLimit = 10;
        _configuration.Tickets.GuildLimit = 1;
        var first = await Open("first", new Actor(1, []), "guild-first");
        var second = await _service.OpenAsync("second", new Actor(2, []), "guild-second");

        Assert.That(first.Success, Is.True);
        Assert.That(second.Success, Is.False);
        Assert.That(_discord.CreateCalls, Is.EqualTo(1));
    }

    [Test]
    public async Task ConcurrentDuplicateInteractionCreatesOnlyOneTicket()
    {
        var requester = new Actor(1, []);
        var attempts = Enumerable.Range(0, 12)
            .Select(_ => _service.OpenAsync("first", requester, "same-interaction"));
        var results = await Task.WhenAll(attempts);

        Assert.That(results.All(x => x.Success), Is.True);
        Assert.That(_discord.CreateCalls, Is.EqualTo(1));
        Assert.That(_store.Tickets, Has.Count.EqualTo(1));
        Assert.That(_store.Tickets[0].State, Is.EqualTo(TicketState.Open));
    }

    [Test]
    public async Task ConcurrentDifferentInteractionsCannotPassMemberLimitTogether()
    {
        _configuration.Tickets.MemberLimit = 1;
        var requester = new Actor(1, []);
        var results = await Task.WhenAll(
            _service.OpenAsync("first", requester, "parallel-one"),
            _service.OpenAsync("second", requester, "parallel-two"));

        Assert.That(results.Count(x => x.Success), Is.EqualTo(1));
        Assert.That(_discord.CreateCalls, Is.EqualTo(1));
        Assert.That(_store.Tickets, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task RetryFindsChannelCreatedBeforeInterruptedDatabaseBinding()
    {
        _store.FailNextSaveAction = "channel-bound";
        _store.FailNextSaveWithCancellation = true;
        var requester = new Actor(1, []);
        using var cancellation = new CancellationTokenSource();
        _store.BeforeSaveCancellation = cancellation.Cancel;

        Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await _service.OpenAsync("first", requester, "interrupted-create", cancellation.Token);
        });
        var recovered = await _service.OpenAsync("first", requester, "interrupted-create");

        Assert.Multiple(() =>
        {
            Assert.That(recovered.Success, Is.True);
            Assert.That(_discord.CreateCalls, Is.EqualTo(1));
            Assert.That(recovered.Ticket!.ChannelId, Is.EqualTo(ChannelOne));
            Assert.That(_store.Tickets, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task CleanupWaitsForClosedTicketDeadlineAndLeavesOpenTicketsAlone()
    {
        var closed = await Open("first", new Actor(1, []), "cleanup-closed");
        await Open("second", new Actor(2, []), "cleanup-open");
        await _service.CloseAsync(ChannelOne, Support);

        _clock.Advance(TimeSpan.FromDays(7) - TimeSpan.FromSeconds(1));
        await _service.MaintainAsync();
        Assert.That(_discord.DeleteCalls, Is.Zero);

        _clock.Advance(TimeSpan.FromSeconds(1));
        await _service.MaintainAsync();

        Assert.Multiple(() =>
        {
            Assert.That(closed.Success, Is.True);
            Assert.That(_discord.DeletedChannels, Is.EquivalentTo(new[] { ChannelOne }));
            Assert.That(_store.Tickets.Single(t => t.ChannelId == ChannelOne).State, Is.EqualTo(TicketState.Deleted));
            Assert.That(_store.Tickets.Single(t => t.ChannelId == ChannelOne + 1).State, Is.EqualTo(TicketState.Open));
        });
    }

    [Test]
    public async Task ExportFailureRetainsChannelForRetry()
    {
        await Open("first", new Actor(1, []), "export-fails");
        await _service.CloseAsync(ChannelOne, Support);
        _clock.Advance(TimeSpan.FromDays(7));
        _archive.FailExports = true;

        await _service.MaintainAsync();

        Assert.Multiple(() =>
        {
            Assert.That(_discord.DeleteCalls, Is.Zero);
            Assert.That(_discord.ExistsCalls, Is.EqualTo(1));
            Assert.That(_store.Tickets.Single().State, Is.EqualTo(TicketState.Deleting));
            Assert.That(_store.Tickets.Single().LastError, Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public async Task NewMessageAfterExportInvalidatesSnapshotAndPreventsDeletion()
    {
        await Open("first", new Actor(1, []), "message-during-export");
        await _service.CloseAsync(ChannelOne, Support);
        _clock.Advance(TimeSpan.FromDays(7));
        _discord.LastMessageIdAfterRead = 102;

        await _service.MaintainAsync();

        Assert.Multiple(() =>
        {
            Assert.That(_discord.DeleteCalls, Is.Zero);
            Assert.That(_archive.ExportCalls, Is.EqualTo(1));
            Assert.That(_store.Tickets.Single().ArchivePath, Does.StartWith("/archives/"));
            Assert.That(_store.Tickets.Single().LastError, Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public async Task ReopenClearsCleanupDeadlineAndExpiryBeforeOriginalDeadline()
    {
        await Open("first", new Actor(1, []), "reopen-cancels-cleanup");
        await _service.CloseAsync(ChannelOne, Support);
        var reopened = await _service.ReopenAsync(ChannelOne, Support);
        _clock.Advance(TimeSpan.FromDays(8));
        await _service.MaintainAsync();

        Assert.Multiple(() =>
        {
            Assert.That(reopened.Success, Is.True);
            Assert.That(reopened.Ticket!.DeleteAfter, Is.Null);
            Assert.That(reopened.Ticket.ArchiveExpiresAt, Is.Null);
            Assert.That(_store.Tickets.Single().State, Is.EqualTo(TicketState.Open));
            Assert.That(_discord.DeleteCalls, Is.Zero);
        });
    }

    [Test]
    public async Task InterruptedCloseAndReopenOperationsAreReconciled()
    {
        await Open("first", new Actor(1, []), "transition-recovery");
        _discord.FailNextClose = true;
        Assert.ThrowsAsync<InvalidOperationException>(async () => { await _service.CloseAsync(ChannelOne, Support); });
        Assert.That(_store.Tickets.Single().State, Is.EqualTo(TicketState.Closing));
        await _service.MaintainAsync();
        Assert.That(_store.Tickets.Single().State, Is.EqualTo(TicketState.Closed));

        _discord.FailNextOpen = true;
        Assert.ThrowsAsync<InvalidOperationException>(async () => { await _service.ReopenAsync(ChannelOne, Support); });
        Assert.That(_store.Tickets.Single().State, Is.EqualTo(TicketState.Reopening));
        await _service.MaintainAsync();

        Assert.Multiple(() =>
        {
            Assert.That(_store.Tickets.Single().State, Is.EqualTo(TicketState.Open));
            Assert.That(_discord.CloseCalls, Is.EqualTo(2));
            Assert.That(_discord.OpenCalls, Is.EqualTo(3));
        });
    }

    [Test]
    public async Task HoldBlocksClosedChannelCleanup()
    {
        await Open("first", new Actor(1, []), "hold-cleanup");
        await _service.CloseAsync(ChannelOne, Support);
        var held = await _service.SetHoldAsync(ChannelOne, Support, true);
        _clock.Advance(TimeSpan.FromDays(8));
        await _service.MaintainAsync();

        Assert.Multiple(() =>
        {
            Assert.That(held.Success, Is.True);
            Assert.That(_discord.DeleteCalls, Is.Zero);
            Assert.That(_store.Tickets.Single().State, Is.EqualTo(TicketState.Closed));
        });
    }

    [Test]
    public async Task HoldBlocksExpirationOfAnArchivedDeletedTicket()
    {
        var opened = await Open("first", new Actor(1, []), "hold-expiration");
        await _service.CloseAsync(ChannelOne, Support);
        _clock.Advance(TimeSpan.FromDays(7));
        await _service.MaintainAsync();
        var deleted = _store.Tickets.Single();
        _store.Replace(deleted with { Hold = true });
        _clock.Advance(TimeSpan.FromDays(91));

        await _service.MaintainAsync();

        Assert.Multiple(() =>
        {
            Assert.That(opened.Success, Is.True);
            Assert.That(_store.Tickets.Single().State, Is.EqualTo(TicketState.Deleted));
            Assert.That(_store.Tickets.Single().ArchivePath, Is.Not.Null);
            Assert.That(_archive.DeletedPaths, Is.Empty);
        });
    }

    [Test]
    public async Task ExpiredArchiveIsRemovedAfterTicketChannelWasDeleted()
    {
        await Open("first", new Actor(1, []), "archive-expiration");
        await _service.CloseAsync(ChannelOne, Support);
        _clock.Advance(TimeSpan.FromDays(7));
        await _service.MaintainAsync();
        var archivePath = _store.Tickets.Single().ArchivePath;
        Assert.That(_store.Tickets.Single().State, Is.EqualTo(TicketState.Deleted));

        _clock.Advance(TimeSpan.FromDays(90));
        await _service.MaintainAsync();

        Assert.That(_archive.DeletedPaths, Is.EquivalentTo(new[] { archivePath }));
        Assert.That(_store.Tickets.Single().ArchivePath, Is.Null);
    }

    [TestCase(TicketState.Open)]
    [TestCase(TicketState.Creating)]
    [TestCase(TicketState.Closing)]
    [TestCase(TicketState.Reopening)]
    [TestCase(TicketState.Closed)]
    [TestCase(TicketState.Deleting)]
    public async Task MissingBoundChannelIsTerminalAndReleasesAllTicketLimits(TicketState state)
    {
        _configuration.Tickets.MemberLimit = 1;
        _configuration.Tickets.GuildLimit = 1;
        _configuration.Tickets.Panels[0].OpenLimit = 1;
        var requester = new Actor(1, []);
        var opened = await Open("first", requester, "externally-removed");
        _store.Replace(opened.Ticket! with { State = state });
        _discord.MissingChannels.Add(ChannelOne);

        await _service.MaintainAsync();
        var removed = _store.Tickets.Single();
        var replacement = await Open("first", requester, "replacement");

        Assert.Multiple(() =>
        {
            Assert.That(removed.State, Is.EqualTo(TicketState.Deleted));
            Assert.That(removed.LastError, Does.Contain("removed externally"));
            Assert.That(removed.ClosedAt, Is.EqualTo(_clock.GetUtcNow()));
            Assert.That(removed.ArchiveExpiresAt, Is.EqualTo(_clock.GetUtcNow().AddDays(90)));
            Assert.That(replacement.Success, Is.True);
            Assert.That(_discord.DeleteCalls, Is.Zero, "reconciliation must not delete any channel");
            Assert.That(_archive.ExportCalls, Is.Zero, "a missing channel cannot be archived");
        });
    }

    [Test]
    public async Task ChannelLookupFailureRetainsActiveTicketAndItsCapacity()
    {
        _configuration.Tickets.MemberLimit = 1;
        await Open("first", new Actor(1, []), "lookup-failure");
        _discord.FailNextExists = true;

        await _service.MaintainAsync();
        var ticket = _store.Tickets.Single();
        var replacement = await Open("first", new Actor(1, []), "must-not-replace");

        Assert.That(ticket.State, Is.EqualTo(TicketState.Open));
        Assert.That(ticket.LastError, Does.Contain("Maintenance failed"));
        Assert.That(replacement.Success, Is.False);
        Assert.That(_discord.DeleteCalls, Is.Zero);
    }

    [Test]
    public async Task HeldArchiveSurvivesReconciliationOfMissingOpenChannel()
    {
        await Open("first", new Actor(1, []), "held-missing");
        await _service.ExportAsync(ChannelOne, Support);
        await _service.SetHoldAsync(ChannelOne, Support, true);
        _discord.MissingChannels.Add(ChannelOne);

        await _service.MaintainAsync();
        _clock.Advance(TimeSpan.FromDays(91));
        await _service.MaintainAsync();

        Assert.That(_store.Tickets.Single().State, Is.EqualTo(TicketState.Deleted));
        Assert.That(_store.Tickets.Single().Hold, Is.True);
        Assert.That(_store.Tickets.Single().ArchivePath, Is.Not.Null);
        Assert.That(_archive.DeletedPaths, Is.Empty);
        Assert.That(_discord.DeleteCalls, Is.Zero);
    }

    [Test]
    public async Task MaintenanceReleasesItsLockBetweenTicketsAndReadsUpdatedState()
    {
        await Open("first", new Actor(1, []), "batch-first");
        await Open("first", new Actor(2, []), "batch-second");
        await _service.CloseAsync(ChannelOne + 1, Support);
        _clock.Advance(TimeSpan.FromDays(8));
        var firstLookup = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseLookup = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<TicketResult>? hold = null;
        var mutationCompletedBetweenRecords = false;
        _discord.BeforeExistsAsync = async ticket =>
        {
            if (ticket.ChannelId == ChannelOne)
            {
                firstLookup.SetResult();
                await releaseLookup.Task;
            }
            else
            {
                // The queued mutation must finish before maintenance performs another remote operation.
                Assert.That((await hold!.WaitAsync(TimeSpan.FromSeconds(2))).Success, Is.True);
                mutationCompletedBetweenRecords = true;
            }
        };
        var maintenance = _service.MaintainAsync();
        try
        {
            await firstLookup.Task.WaitAsync(TimeSpan.FromSeconds(2));
            hold = _service.SetHoldAsync(ChannelOne + 1, Support, true);
            releaseLookup.SetResult();
            await maintenance.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.That(mutationCompletedBetweenRecords, Is.True, "queued commands must run during the batch, not only when the batch ends");
            Assert.That(_store.Tickets.All(ticket => ticket.LastError is null), Is.True);
            Assert.That(_store.Tickets.Single(ticket => ticket.ChannelId == ChannelOne + 1).Hold, Is.True);
            Assert.That(_store.Tickets.Single(ticket => ticket.ChannelId == ChannelOne + 1).State, Is.EqualTo(TicketState.Closed));
            Assert.That(_discord.DeleteCalls, Is.Zero);
        }
        finally
        {
            releaseLookup.TrySetResult();
            await maintenance;
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ManualExportReleasesStoreLockDuringRemoteWorkAndPreservesStaffChanges(bool blockArchive)
    {
        await Open("first", new Actor(1, []), "manual-export");
        var remoteStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseRemote = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task Block(Ticket ticket, CancellationToken ct)
        {
            remoteStarted.TrySetResult();
            await releaseRemote.Task.WaitAsync(ct);
        }
        if (blockArchive) _archive.BeforeExportAsync = Block;
        else _discord.BeforeTranscriptAsync = Block;
        var export = _service.ExportAsync(ChannelOne, Support);
        try
        {
            await remoteStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
            var duplicate = await _service.ExportAsync(ChannelOne, Support).WaitAsync(TimeSpan.FromSeconds(2));
            var close = await _service.CloseAsync(ChannelOne, Support).WaitAsync(TimeSpan.FromSeconds(2));
            var hold = await _service.SetHoldAsync(ChannelOne, Support, true).WaitAsync(TimeSpan.FromSeconds(2));
            var other = await Open("second", new Actor(2, []), "other-during-export").WaitAsync(TimeSpan.FromSeconds(2));
            Assert.Multiple(() =>
            {
                Assert.That(duplicate.Success, Is.False);
                Assert.That(duplicate.Message, Does.Contain("export is already in progress"));
                Assert.That(close.Success, Is.True);
                Assert.That(hold.Success, Is.True);
                Assert.That(other.Success, Is.True);
                Assert.That(export.IsCompleted, Is.False);
            });
            releaseRemote.SetResult();
            var result = await export.WaitAsync(TimeSpan.FromSeconds(2));
            var current = _store.Tickets.Single(t => t.ChannelId == ChannelOne);
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(current.State, Is.EqualTo(TicketState.Closed));
                Assert.That(current.Hold, Is.True);
                Assert.That(current.DeleteAfter, Is.EqualTo(_clock.GetUtcNow().AddDays(7)));
                Assert.That(current.ArchiveExpiresAt, Is.EqualTo(_clock.GetUtcNow().AddDays(90)));
                Assert.That(current.ArchivePath, Is.Not.Null);
                Assert.That(_archive.ExportCalls, Is.EqualTo(1));
            });
        }
        finally { releaseRemote.TrySetResult(); await export; }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task CleanupSkipsOnlyTheTicketBeingManuallyExported(bool blockArchive)
    {
        await Open("first", new Actor(1, []), "export-before-cleanup");
        await Open("second", new Actor(2, []), "other-cleanup");
        await _service.CloseAsync(ChannelOne, Support);
        await _service.CloseAsync(ChannelOne + 1, Support);
        _clock.Advance(TimeSpan.FromDays(8));
        var remoteStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseRemote = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task Block(Ticket ticket, CancellationToken ct)
        {
            if (ticket.ChannelId != ChannelOne) return;
            remoteStarted.TrySetResult();
            await releaseRemote.Task.WaitAsync(ct);
        }
        if (blockArchive) _archive.BeforeExportAsync = Block;
        else _discord.BeforeTranscriptAsync = Block;
        var export = _service.ExportAsync(ChannelOne, Support);
        try
        {
            await remoteStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
            await _service.MaintainAsync().WaitAsync(TimeSpan.FromSeconds(2));
            Assert.That(_discord.DeletedChannels, Is.EqualTo(new[] { ChannelOne + 1 }));
            Assert.That(export.IsCompleted, Is.False);
            releaseRemote.SetResult();
            Assert.That((await export.WaitAsync(TimeSpan.FromSeconds(2))).Success, Is.True);
            await _service.MaintainAsync();
            Assert.That(_discord.DeletedChannels, Is.EquivalentTo(new[] { ChannelOne, ChannelOne + 1 }));
        }
        finally { releaseRemote.TrySetResult(); await export; }
    }

    [Test]
    public async Task FailedManualExportReleasesItsCleanupGuardForRetry()
    {
        await Open("first", new Actor(1, []), "failed-manual-export");
        _archive.FailExports = true;
        Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.ExportAsync(ChannelOne, Support));
        _archive.FailExports = false;
        Assert.That((await _service.ExportAsync(ChannelOne, Support)).Success, Is.True);
        await _service.CloseAsync(ChannelOne, Support);
        _clock.Advance(TimeSpan.FromDays(8));
        await _service.MaintainAsync();
        Assert.That(_discord.DeletedChannels, Is.EqualTo(new[] { ChannelOne }));
    }

    [Test]
    public async Task CancelledManualExportReleasesItsCleanupGuardForRetry()
    {
        await Open("first", new Actor(1, []), "cancelled-manual-export");
        var remoteStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();
        _discord.BeforeTranscriptAsync = async (_, ct) =>
        {
            remoteStarted.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
        };
        var export = _service.ExportAsync(ChannelOne, Support, cancellation.Token);
        try
        {
            await remoteStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
            cancellation.Cancel();
            Assert.ThrowsAsync<TaskCanceledException>(async () => await export);
            _discord.BeforeTranscriptAsync = null;
            Assert.That((await _service.ExportAsync(ChannelOne, Support)).Success, Is.True);
            await _service.CloseAsync(ChannelOne, Support);
            _clock.Advance(TimeSpan.FromDays(8));
            await _service.MaintainAsync();
            Assert.That(_discord.DeletedChannels, Is.EqualTo(new[] { ChannelOne }));
        }
        finally
        {
            cancellation.Cancel();
            try { await export; } catch (OperationCanceledException) { }
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task CleanupReleasesStoreLockDuringTranscriptAndAttachmentWork(bool blockArchive)
    {
        await Open("first", new Actor(1, []), "cleanup-unlocked");
        await _service.CloseAsync(ChannelOne, Support);
        _clock.Advance(TimeSpan.FromDays(8));
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task Block(Ticket _, CancellationToken ct)
        {
            started.TrySetResult();
            await release.Task.WaitAsync(ct);
        }
        if (blockArchive) _archive.BeforeExportAsync = Block;
        else _discord.BeforeTranscriptAsync = Block;
        var maintenance = _service.MaintainAsync();
        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
            var other = await Open("second", new Actor(2, []), "other-during-cleanup").WaitAsync(TimeSpan.FromSeconds(2));
            Assert.That(other.Success, Is.True);
            Assert.That((await _service.CloseAsync(ChannelOne + 1, Support).WaitAsync(TimeSpan.FromSeconds(2))).Success, Is.True);
            Assert.That((await _service.SetHoldAsync(ChannelOne + 1, Support, true).WaitAsync(TimeSpan.FromSeconds(2))).Success, Is.True);
            // Another sweep must skip the claimed cleanup rather than overwrite its archive or deadlock.
            await _service.MaintainAsync().WaitAsync(TimeSpan.FromSeconds(2));
            Assert.That(_discord.DeleteCalls, Is.Zero);
            release.SetResult();
            await maintenance.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.That(_discord.DeletedChannels, Is.EqualTo(new[] { ChannelOne }));
            Assert.That(_store.Tickets.Single(t => t.ChannelId == ChannelOne + 1).Hold, Is.True);
        }
        finally { release.TrySetResult(); await maintenance; }
    }

    [TestCase("edit", true)]
    [TestCase("edit", false)]
    [TestCase("delete", true)]
    [TestCase("delete", false)]
    public async Task CleanupDeniesRequesterMutationsAfterVerificationUntilChannelDeletion(string mutation, bool closedRequesterCanRead)
    {
        _configuration.Tickets.ClosedRequesterCanRead = closedRequesterCanRead;
        await Open("first", new Actor(1, []), "requester-freeze");
        await _service.CloseAsync(ChannelOne, Support);
        _clock.Advance(TimeSpan.FromDays(8));
        IReadOnlyCollection<Overwrite> actual = DiscordOperations.BuildOverwrites(1, 2, 1, [SupportRole], [],
            closedRequesterCanRead, false, true);
        _discord.BeforeFreezeAsync = async (ticket, _) =>
        {
            var expected = DiscordOperations.BuildFrozenOverwrites(ticket, 1, 2, [SupportRole], actual);
            await DiscordOperations.SynchronizeTicketOverwritesAsync(ChannelOne, expected, actual,
                value => { actual = value; return Task.CompletedTask; }, () => Task.FromResult(actual));
        };
        var old = new TranscriptMessage(100, 1, "Requester", "original", _clock.GetUtcNow(), []);
        var newest = old with { Id = 101, Content = "newest" };
        _discord.Snapshot = new([old, newest], 101);
        void AssertFrozen()
        {
            var requester = actual.Single(item => item.TargetType == PermissionTarget.User && item.TargetId == 1).Permissions;
            Assert.That(requester.ViewChannel, Is.EqualTo(PermValue.Deny));
            Assert.That(requester.ReadMessageHistory, Is.EqualTo(PermValue.Deny));
            Assert.That(actual.Single(item => item.TargetType == PermissionTarget.Role && item.TargetId == SupportRole)
                .Permissions.ViewChannel, Is.EqualTo(PermValue.Allow), "Staff must still be able to issue a hold.");
        }
        var scans = 0;
        _discord.BeforeTranscriptAsync = (_, _) => { scans++; AssertFrozen(); return Task.CompletedTask; };
        _archive.BeforeExportAsync = (_, _) => { AssertFrozen(); return Task.CompletedTask; };
        var blocked = 0;
        _discord.BeforeLastMessageIdAsync = (_, _) =>
        {
            Assert.That(scans, Is.EqualTo(2), "Attempt the mutation after the verification cursor has passed the older message.");
            var requester = actual.Single(item => item.TargetType == PermissionTarget.User && item.TargetId == 1).Permissions;
            if (requester.ViewChannel == PermValue.Deny) blocked++;
            else _discord.Snapshot = mutation == "delete" ? new([newest], 101) : new([old with { Content = "tampered" }, newest], 101);
            return Task.CompletedTask;
        };
        _discord.BeforeDeleteAsync = (_, _) => { AssertFrozen(); return Task.CompletedTask; };

        await _service.MaintainAsync();

        Assert.That(blocked, Is.EqualTo(1));
        Assert.That(_discord.Snapshot.Messages, Is.EqualTo(new[] { old, newest }));
        Assert.That(_discord.DeletedChannels, Is.EqualTo(new[] { ChannelOne }));
        Assert.That(_store.Tickets.Single().State, Is.EqualTo(TicketState.Deleted));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task CleanupRequiresVerifiedFreezeBeforeTranscriptWorkAndRetriesFailures(bool rejected)
    {
        await Open("first", new Actor(1, []), "freeze-readback-failure");
        await _service.CloseAsync(ChannelOne, Support);
        _clock.Advance(TimeSpan.FromDays(8));
        IReadOnlyCollection<Overwrite> actual = DiscordOperations.BuildOverwrites(1, 2, 1, [SupportRole], [], true, false, true);
        _discord.BeforeFreezeAsync = async (ticket, _) =>
        {
            var expected = DiscordOperations.BuildFrozenOverwrites(ticket, 1, 2, [SupportRole], actual);
            await DiscordOperations.SynchronizeTicketOverwritesAsync(ChannelOne, expected, actual,
                value => rejected ? Task.FromException(new InvalidOperationException("Discord denied the freeze.")) : Task.CompletedTask,
                () => Task.FromResult(actual));
        };
        var reads = 0;
        _discord.BeforeTranscriptAsync = (_, _) => { reads++; return Task.CompletedTask; };
        await _service.MaintainAsync();
        Assert.That(reads, Is.Zero);
        Assert.That(_archive.ExportCalls, Is.Zero);
        Assert.That(_discord.DeleteCalls, Is.Zero);
        Assert.That(_store.Tickets.Single().State, Is.EqualTo(TicketState.Deleting));
        Assert.That(_store.Tickets.Single().LastError, Does.Contain("Maintenance failed"));

        _discord.BeforeFreezeAsync = async (ticket, _) =>
        {
            var expected = DiscordOperations.BuildFrozenOverwrites(ticket, 1, 2, [SupportRole], actual);
            await DiscordOperations.SynchronizeTicketOverwritesAsync(ChannelOne, expected, actual,
                value => { actual = value; return Task.CompletedTask; }, () => Task.FromResult(actual));
        };
        await _service.MaintainAsync();
        Assert.That(reads, Is.EqualTo(2));
        Assert.That(actual.Single(item => item.TargetType == PermissionTarget.User && item.TargetId == 1)
            .Permissions.ViewChannel, Is.EqualTo(PermValue.Deny));
        Assert.That(_discord.DeletedChannels, Is.EqualTo(new[] { ChannelOne }));
    }

    [Test]
    public async Task FailedCleanupKeepsRequesterFrozenAndStaffCanHoldThenReleaseForRetry()
    {
        await Open("first", new Actor(1, []), "failed-frozen-hold");
        await _service.CloseAsync(ChannelOne, Support);
        _clock.Advance(TimeSpan.FromDays(8));
        IReadOnlyCollection<Overwrite> actual = DiscordOperations.BuildOverwrites(1, 2, 1, [SupportRole], [], true, false, true);
        _discord.BeforeFreezeAsync = async (ticket, _) =>
        {
            var expected = DiscordOperations.BuildFrozenOverwrites(ticket, 1, 2, [SupportRole], actual);
            await DiscordOperations.SynchronizeTicketOverwritesAsync(ChannelOne, expected, actual,
                value => { actual = value; return Task.CompletedTask; }, () => Task.FromResult(actual));
        };
        _archive.FailExports = true;
        await _service.MaintainAsync();
        Assert.That(_store.Tickets.Single().State, Is.EqualTo(TicketState.Deleting));
        Assert.That(_discord.DeleteCalls, Is.Zero);
        Assert.That((await _service.SetHoldAsync(ChannelOne, Support, true)).Success, Is.True);
        Assert.That((await _service.ReopenAsync(ChannelOne, Support)).Success, Is.False,
            "A hold preserves Deleting state and must not reopen requester access during an unfinished archive.");
        var freezes = _discord.FreezeCalls;
        var reconciles = _discord.ReconciledTickets.Count;
        await _service.ReconcileRetainedPermissionsAsync();
        await _service.MaintainAsync();
        Assert.That(_discord.FreezeCalls, Is.EqualTo(freezes));
        Assert.That(_discord.ReconciledTickets.Count, Is.EqualTo(reconciles));
        Assert.That(actual.Single(item => item.TargetType == PermissionTarget.User && item.TargetId == 1)
            .Permissions.ViewChannel, Is.EqualTo(PermValue.Deny));
        Assert.That(actual.Single(item => item.TargetType == PermissionTarget.Role && item.TargetId == SupportRole)
            .Permissions.ViewChannel, Is.EqualTo(PermValue.Allow));
        Assert.That((await _service.SetHoldAsync(ChannelOne, Support, false)).Success, Is.True);
        _archive.FailExports = false;
        await _service.MaintainAsync();
        Assert.That(_discord.DeletedChannels, Is.EqualTo(new[] { ChannelOne }));
        Assert.That(_store.Tickets.Single().State, Is.EqualTo(TicketState.Deleted));
    }

    [TestCase("edit")]
    [TestCase("delete")]
    [TestCase("embed")]
    [TestCase("attachment")]
    [TestCase("poll")]
    [TestCase("sticker")]
    [TestCase("components")]
    [TestCase("forwarded")]
    [TestCase("reactions")]
    [TestCase("reference")]
    public async Task CleanupRejectsOlderMessageChangesEvenWhenNewestIdIsUnchanged(string change)
    {
        await Open("first", new Actor(1, []), "old-message-change");
        await _service.CloseAsync(ChannelOne, Support);
        _clock.Advance(TimeSpan.FromDays(8));
        var old = new TranscriptMessage(100, 1, "Requester", "original", _clock.GetUtcNow(),
            [new TranscriptAttachment(5, "evidence.txt", "https://cdn.discordapp.com/attachments/5?ex=old", 10)], "[]");
        var newest = old with { Id = 101, Content = "newest", Attachments = [] };
        _discord.Snapshot = new([old, newest], 101);
        _archive.BeforeExportAsync = (_, _) =>
        {
            var edited = change switch
            {
                "edit" => old with { Content = "edited" },
                "embed" => old with { EmbedsJson = "[{modified:true}]" },
                "attachment" => old with { Attachments = [] },
                "poll" or "sticker" or "components" or "forwarded" or "reactions" or "reference" => old with { MetadataJson = "{\"" + change + "\":\"changed\"}" },
                _ => old
            };
            _discord.Snapshot = change == "delete" ? new([newest], 101) : new([edited, newest], 101);
            return Task.CompletedTask;
        };
        await _service.MaintainAsync();
        Assert.Multiple(() =>
        {
            Assert.That(_discord.DeleteCalls, Is.Zero);
            Assert.That(_store.Tickets.Single().State, Is.EqualTo(TicketState.Deleting));
            Assert.That(_store.Tickets.Single().ArchivePath, Is.Not.Null);
            Assert.That(_store.Tickets.Single().LastError, Is.Not.Null);
        });
        _archive.BeforeExportAsync = null;
        await _service.MaintainAsync();
        Assert.That(_discord.DeletedChannels, Is.EqualTo(new[] { ChannelOne }), "retry must archive the current complete transcript");
    }

    [Test]
    public async Task CleanupAcceptsRefreshedAttachmentSignaturesAndMessageOrder()
    {
        await Open("first", new Actor(1, []), "signed-attachment");
        await _service.CloseAsync(ChannelOne, Support);
        _clock.Advance(TimeSpan.FromDays(8));
        var attachment = new TranscriptAttachment(5, "evidence.txt", "https://cdn.discordapp.com/attachments/5?ex=old", 10);
        var old = new TranscriptMessage(100, 1, "Requester", "unchanged", _clock.GetUtcNow(), [attachment]);
        var newest = old with { Id = 101, Attachments = [] };
        _discord.Snapshot = new([old, newest], 101);
        _archive.BeforeExportAsync = (_, _) =>
        {
            _discord.Snapshot = new([newest, old with { Attachments = [attachment with { Url = "https://cdn.discordapp.com/attachments/5?ex=refreshed&hm=new" }] }], 101);
            return Task.CompletedTask;
        };
        await _service.MaintainAsync();
        Assert.That(_discord.DeletedChannels, Is.EqualTo(new[] { ChannelOne }));
    }

    [Test]
    public async Task AttachmentTimeoutRetainsFailedTicketAndContinuesCleaningLaterTickets()
    {
        await Open("first", new Actor(1, []), "timeout-first");
        await Open("second", new Actor(2, []), "timeout-second");
        await _service.CloseAsync(ChannelOne, Support);
        await _service.CloseAsync(ChannelOne + 1, Support);
        _clock.Advance(TimeSpan.FromDays(8));
        _archive.BeforeExportAsync = (ticket, _) => ticket.ChannelId == ChannelOne
            ? Task.FromException(new TaskCanceledException("Simulated CDN request timeout.")) : Task.CompletedTask;
        await _service.MaintainAsync();
        Assert.Multiple(() =>
        {
            Assert.That(_store.Tickets.Single(t => t.ChannelId == ChannelOne).State, Is.EqualTo(TicketState.Deleting));
            Assert.That(_store.Tickets.Single(t => t.ChannelId == ChannelOne).LastError, Is.Not.Null);
            Assert.That(_discord.DeletedChannels, Is.EqualTo(new[] { ChannelOne + 1 }));
        });
        _archive.BeforeExportAsync = null;
        await _service.MaintainAsync();
        Assert.That(_discord.DeletedChannels, Is.EquivalentTo(new[] { ChannelOne, ChannelOne + 1 }));
    }

    [Test]
    public async Task CallerCancellationStopsCleanupAndReleasesExportGuard()
    {
        await Open("first", new Actor(1, []), "cancel-cleanup");
        await _service.CloseAsync(ChannelOne, Support);
        _clock.Advance(TimeSpan.FromDays(8));
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();
        _archive.BeforeExportAsync = async (_, ct) =>
        {
            started.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
        };
        var maintenance = _service.MaintainAsync(cancellation.Token);
        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
            cancellation.Cancel();
            Assert.ThrowsAsync<TaskCanceledException>(async () => await maintenance);
            Assert.That(_store.Tickets.Single().LastError, Is.Null);
            _archive.BeforeExportAsync = null;
            await _service.MaintainAsync();
            Assert.That(_discord.DeletedChannels, Is.EqualTo(new[] { ChannelOne }));
        }
        finally { cancellation.Cancel(); try { await maintenance; } catch (OperationCanceledException) { } }
    }

    [Test]
    public async Task InternalCreationCancellationIsRecoverableWithoutCancellingCaller()
    {
        _store.FailNextSaveAction = "channel-bound";
        _store.FailNextSaveWithCancellation = true;
        var result = await _service.OpenAsync("first", new Actor(1, []), "internal-create-timeout");
        Assert.That(result.Success, Is.False);
        Assert.That(_store.Tickets.Single().LastError, Is.Not.Null);
        Assert.That((await _service.OpenAsync("first", new Actor(1, []), "internal-create-timeout")).Success, Is.True);
        Assert.That(_discord.CreateCalls, Is.EqualTo(1));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ArchiveOwnershipMustCommitBeforeAnyTranscriptWork(bool cleanup)
    {
        await Open("first", new Actor(1, []), "ownership-save-failure");
        await _service.CloseAsync(ChannelOne, Support);
        _clock.Advance(TimeSpan.FromDays(8));
        _store.FailNextSaveAction = "archive-pending";
        _discord.BeforeTranscriptAsync = (_, _) => throw new AssertionException("No remote transcript work before durable ownership.");
        if (cleanup) await _service.MaintainAsync();
        else Assert.ThrowsAsync<InvalidOperationException>(() => _service.ExportAsync(ChannelOne, Support));
        Assert.That(_archive.ExportCalls, Is.Zero);
        Assert.That(_store.Tickets.Single().ArchivePath, Is.Null);
        _discord.BeforeTranscriptAsync = null;
        if (cleanup) await _service.MaintainAsync();
        else Assert.That((await _service.ExportAsync(ChannelOne, Support)).Success, Is.True);
        Assert.That(_store.Tickets.Single().ArchiveComplete, Is.True);
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    public async Task PublishedArchiveRemainsTrackedAfterFailedCompletionSaveAndExternalChannelRemoval(bool cleanup, bool hold)
    {
        var root = Path.Combine(Path.GetTempPath(), "swlor-retention-tests", Guid.NewGuid().ToString("N"));
        try
        {
            _configuration.Tickets.ArchiveDirectory = root;
            _configuration.Tickets.CopyAttachments = false;
            using var http = new HttpClient();
            var files = new FileTranscriptArchive(_configuration, http);
            _service = new TicketService(_configuration, _store, _discord, files, _clock);
            await Open("first", new Actor(1, []), "published-before-save");
            await _service.CloseAsync(ChannelOne, Support);
            if (hold) await _service.SetHoldAsync(ChannelOne, Support, true);
            _clock.Advance(TimeSpan.FromDays(8));
            _store.FailNextSaveAction = cleanup ? "cleanup-exported" : "exported";
            if (cleanup) await _service.MaintainAsync();
            else Assert.ThrowsAsync<InvalidOperationException>(() => _service.ExportAsync(ChannelOne, Support));
            var pending = _store.Tickets.Single();
            Assert.That(pending.ArchivePath, Is.EqualTo(files.GetArchivePath(pending)));
            Assert.That(Directory.GetFiles(pending.ArchivePath!, "transcript.html", SearchOption.AllDirectories), Has.Length.EqualTo(1));
            Assert.That(pending.ArchiveComplete, Is.False, "publication and its successful completion commit are distinct");
            _discord.MissingChannels.Add(ChannelOne);
            // Restart loses the in-process guard but preserves the committed ownership record.
            _service = new TicketService(_configuration, _store, _discord, files, _clock);
            await _service.MaintainAsync();
            Assert.That(_store.Tickets.Single().State, Is.EqualTo(TicketState.Deleted));
            _clock.Advance(TimeSpan.FromDays(91));
            await _service.MaintainAsync();
            Assert.That(Directory.Exists(pending.ArchivePath!), Is.EqualTo(hold));
            Assert.That(_store.Tickets.Single().ArchivePath, hold ? Is.Not.Null : Is.Null);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public async Task TranscriptDeliveryKeepsArchiveProtectedWithoutHoldingTheTicketStoreLock()
    {
        await Open("first", new Actor(1, []), "protected-download");
        await _service.CloseAsync(ChannelOne, Support);
        _clock.Advance(TimeSpan.FromDays(8));
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var export = _service.ExportAsync(ChannelOne, Support, deliver: async (ticket, ct) =>
        {
            Assert.That(ticket.ArchiveComplete, Is.True);
            started.SetResult();
            await release.Task.WaitAsync(ct);
        });
        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.That((await Open("second", new Actor(2, []), "other-during-download").WaitAsync(TimeSpan.FromSeconds(2))).Success, Is.True);
            await _service.MaintainAsync().WaitAsync(TimeSpan.FromSeconds(2));
            Assert.That(_discord.DeleteCalls, Is.Zero, "cleanup must retain the archive while files are uploaded");
            release.SetResult();
            Assert.That((await export.WaitAsync(TimeSpan.FromSeconds(2))).Success, Is.True);
            await _service.MaintainAsync();
            Assert.That(_discord.DeletedChannels, Is.EqualTo(new[] { ChannelOne }));
        }
        finally { release.TrySetResult(); await export; }
    }

    [Test]
    public async Task FailedTranscriptDeliveryReleasesGuardButPreservesCompleteOwnedArchive()
    {
        await Open("first", new Actor(1, []), "failed-download");
        Assert.ThrowsAsync<IOException>(() => _service.ExportAsync(ChannelOne, Support,
            deliver: (_, _) => Task.FromException(new IOException("Upload failed."))));
        Assert.That(_store.Tickets.Single().ArchiveComplete, Is.True);
        Assert.That((await _service.ExportAsync(ChannelOne, Support)).Success, Is.True);
    }

    [TestCase("member", true)]
    [TestCase("member", false)]
    [TestCase("member", null)]
    [TestCase("panel", true)]
    [TestCase("panel", false)]
    [TestCase("panel", null)]
    [TestCase("guild", true)]
    [TestCase("guild", false)]
    [TestCase("guild", null)]
    public async Task ReopenUsesRequestersCurrentRoleAndExplicitBypassForEachLimit(string scope, bool? enabled)
    {
        await PrepareFullReopenLimit(scope);
        _discord.CurrentActors[1] = new Actor(1, [901]);
        SetBypassScope(scope, enabled);

        var result = await _service.ReopenAsync(ChannelOne, Support);

        Assert.That(result.Success, Is.EqualTo(enabled == true));
        Assert.That(_discord.ActorLookups.TakeLast(2), Is.EqualTo(new ulong[] { Support.UserId, 1 }));
        Assert.That(_store.Tickets.Single(t => t.ChannelId == ChannelOne).State,
            Is.EqualTo(enabled == true ? TicketState.Open : TicketState.Closed));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task ReopenRefreshesBypassRoleAddedOrRemovedSinceOpening(bool currentlyHasRole)
    {
        var openingActor = new Actor(1, currentlyHasRole ? [] : [901]);
        _configuration.Tickets.BypassRoleIds = [901];
        await Open("first", openingActor, "stale-requester");
        await _service.CloseAsync(ChannelOne, Support);
        await Open("second", new Actor(1, []), "active-requester");
        _configuration.Tickets.MemberLimit = 1;
        _configuration.Tickets.BypassMemberLimit = true;
        _discord.CurrentActors[1] = new Actor(1, currentlyHasRole ? [901] : []);

        var result = await _service.ReopenAsync(ChannelOne, Support);

        Assert.That(result.Success, Is.EqualTo(currentlyHasRole));
        Assert.That(_discord.ActorLookups.TakeLast(2), Is.EqualTo(new ulong[] { Support.UserId, 1 }));
    }

    [Test]
    public async Task ReopeningStaffCannotTransferTheirBypassToTheRequester()
    {
        await PrepareFullReopenLimit("member");
        _configuration.Tickets.BypassMemberLimit = true;
        var privilegedStaff = new Actor(Support.UserId, [SupportRole, 901], IsGuildOwner: true);

        var result = await _service.ReopenAsync(ChannelOne, privilegedStaff);

        Assert.That(result.Success, Is.False);
        Assert.That(_discord.ActorLookups.TakeLast(2), Is.EqualTo(new ulong[] { Support.UserId, 1 }));
        Assert.That(_store.Tickets.Single(t => t.ChannelId == ChannelOne).State, Is.EqualTo(TicketState.Closed));
    }

    [Test]
    public async Task ReopenFailsClosedWhenRequesterHasLeftTheGuild()
    {
        await PrepareFullReopenLimit("member");
        _configuration.Tickets.BypassMemberLimit = true;
        _discord.CurrentActors[1] = new Actor(1, [901]);
        _discord.MissingMembers.Add(1);

        Assert.ThrowsAsync<InvalidOperationException>(() => _service.ReopenAsync(ChannelOne, Support));

        Assert.That(_store.Tickets.Single(t => t.ChannelId == ChannelOne).State, Is.EqualTo(TicketState.Closed));
        Assert.That(_discord.OpenCalls, Is.EqualTo(2), "Only initial creation calls may have opened channels.");
    }

    [TestCase("member")]
    [TestCase("panel")]
    [TestCase("guild")]
    public async Task DisabledIntakeReopeningStillEnforcesCurrentOpenTicketLimits(string scope)
    {
        await PrepareFullReopenLimit(scope);
        _configuration.Tickets.Enabled = false;

        var result = await _service.ReopenAsync(ChannelOne, Support);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("open-ticket limit"));
        Assert.That(_store.Tickets.Single(ticket => ticket.ChannelId == ChannelOne).State, Is.EqualTo(TicketState.Closed));
        Assert.That(_discord.OpenCalls, Is.EqualTo(2), "Disabling intake must not bypass reopen capacity checks.");
    }
    private async Task PrepareFullReopenLimit(string scope)
    {
        await Open("first", new Actor(1, []), "reopen-limit-closed");
        await _service.CloseAsync(ChannelOne, Support);
        await Open("first", new Actor(1, []), "reopen-limit-active");
        _configuration.Tickets.BypassRoleIds = [901];
        switch (scope)
        {
            case "member": _configuration.Tickets.MemberLimit = 1; break;
            case "panel": _configuration.Tickets.Panels[0].OpenLimit = 1; break;
            case "guild": _configuration.Tickets.GuildLimit = 1; break;
            default: throw new ArgumentOutOfRangeException(nameof(scope));
        }
    }

    private void SetBypassScope(string scope, bool? value)
    {
        switch (scope)
        {
            case "member": _configuration.Tickets.BypassMemberLimit = value; break;
            case "panel": _configuration.Tickets.BypassPanelLimit = value; break;
            case "guild": _configuration.Tickets.BypassGuildLimit = value; break;
            default: throw new ArgumentOutOfRangeException(nameof(scope));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task StaffCanHoldDeletingTicketDuringRemoteWorkAndReleaseForCleanup(bool blockArchive)
    {
        await Open("first", new Actor(1, []), "hold-deleting");
        await _service.CloseAsync(ChannelOne, Support);
        _clock.Advance(TimeSpan.FromDays(8));
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task Block(Ticket ticket, CancellationToken ct)
        {
            started.TrySetResult();
            await release.Task.WaitAsync(ct);
        }
        if (blockArchive) _archive.BeforeExportAsync = Block;
        else _discord.BeforeTranscriptAsync = Block;
        var cleanup = _service.MaintainAsync();
        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.That(_store.Tickets.Single().State, Is.EqualTo(TicketState.Deleting));
            var unauthorized = await _service.SetHoldAsync(ChannelOne, new Actor(2, []), true).WaitAsync(TimeSpan.FromSeconds(2));
            Assert.That(unauthorized.Success, Is.False);
            Assert.That(_store.Tickets.Single().Hold, Is.False);
            var held = await _service.SetHoldAsync(ChannelOne, Support, true).WaitAsync(TimeSpan.FromSeconds(2));
            Assert.That(held.Success, Is.True);
            release.SetResult();
            await cleanup.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.That(_store.Tickets.Single().Hold, Is.True);
            Assert.That(_store.Tickets.Single().ArchiveComplete, Is.True);
            Assert.That(_discord.DeleteCalls, Is.Zero);
            Assert.That((await _service.SetHoldAsync(ChannelOne, Support, false)).Success, Is.True);
            await _service.MaintainAsync();
            Assert.That(_store.Tickets.Single().State, Is.EqualTo(TicketState.Deleted));
            Assert.That((await _service.SetHoldAsync(ChannelOne, Support, true)).Success, Is.False);
            Assert.That(_discord.DeleteCalls, Is.EqualTo(1));
        }
        finally { release.TrySetResult(); await cleanup; }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task FourMinuteInactivityTimeoutReconcilesLaterTicketsAndReleasesGuardForRetry(bool blockArchive)
    {
        await Open("first", new Actor(1, []), "bounded-first");
        await Open("second", new Actor(2, []), "bounded-second");
        await _service.CloseAsync(ChannelOne, Support);
        _clock.Advance(TimeSpan.FromDays(8));
        _discord.MissingChannels.Add(ChannelOne + 1);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task Block(Ticket ticket, CancellationToken ct)
        {
            if (ticket.ChannelId != ChannelOne) return;
            started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
        }
        if (blockArchive) _archive.BeforeExportAsync = Block;
        else _discord.BeforeTranscriptAsync = Block;
        using var safetyCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var maintenance = _service.MaintainAsync(safetyCancellation.Token);
        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
            _clock.Advance(TimeSpan.FromMinutes(4) - TimeSpan.FromSeconds(1));
            Assert.That(maintenance.IsCompleted, Is.False, "The deadline must not expire early.");
            _clock.Advance(TimeSpan.FromSeconds(1));
            await maintenance.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.That(_store.Tickets.Single(t => t.ChannelId == ChannelOne).State, Is.EqualTo(TicketState.Deleting));
            Assert.That(_store.Tickets.Single(t => t.ChannelId == ChannelOne).LastError, Does.Contain("Maintenance failed"));
            Assert.That(_store.Tickets.Single(t => t.ChannelId == ChannelOne + 1).State, Is.EqualTo(TicketState.Deleted));
            _archive.BeforeExportAsync = null;
            _discord.BeforeTranscriptAsync = null;
            await _service.MaintainAsync();
            Assert.That(_discord.DeletedChannels, Is.EqualTo(new[] { ChannelOne }));
        }
        finally
        {
            safetyCancellation.Cancel();
            try { await maintenance; } catch (OperationCanceledException) { }
        }
    }

    [Test]
    public async Task InterruptedSweepResumesAtLaterTicketBeforeRetryingSlowTicket()
    {
        await Open("first", new Actor(1, []), "rotate-first");
        await Open("second", new Actor(2, []), "rotate-second");
        await _service.CloseAsync(ChannelOne, Support);
        _clock.Advance(TimeSpan.FromDays(8));
        _discord.MissingChannels.Add(ChannelOne + 1);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _discord.BeforeTranscriptAsync = async (_, ct) =>
        {
            started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
        };
        using var cancellation = new CancellationTokenSource();
        var interrupted = _service.MaintainAsync(cancellation.Token);
        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
            cancellation.Cancel();
            Assert.CatchAsync<OperationCanceledException>(async () => await interrupted);
            Assert.That(_store.Tickets.All(t => t.LastError is null), Is.True, "Caller cancellation is not a maintenance failure.");
            var lookups = new List<ulong>();
            _discord.BeforeExistsAsync = ticket =>
            {
                lookups.Add(ticket.ChannelId!.Value);
                return Task.CompletedTask;
            };
            _discord.BeforeTranscriptAsync = null;
            await _service.MaintainAsync();
            Assert.That(lookups, Is.EqualTo(new[] { ChannelOne + 1, ChannelOne }));
            Assert.That(_store.Tickets.All(t => t.State == TicketState.Deleted), Is.True);
            Assert.That(_discord.DeletedChannels, Is.EqualTo(new[] { ChannelOne }));
        }
        finally { cancellation.Cancel(); try { await interrupted; } catch (OperationCanceledException) { } }
    }

    [TestCase(TicketState.Closed, true)]
    [TestCase(TicketState.Closing, true)]
    [TestCase(TicketState.Deleting, true)]
    [TestCase(TicketState.Deleted, true)]
    [TestCase(TicketState.Open, false)]
    [TestCase(TicketState.Reopening, false)]
    [TestCase(TicketState.Creating, false)]
    public async Task OfflineExpirationRemovesOnlyExpiredInactiveArchives(TicketState state, bool expires)
    {
        var opened = await Open("first", new Actor(1, []), "offline-expire");
        var path = _archive.GetArchivePath(opened.Ticket!);
        _store.Replace(opened.Ticket! with
        {
            State = state, ArchivePath = path, ArchiveComplete = true,
            ArchiveExpiresAt = _clock.GetUtcNow().AddSeconds(-1)
        });
        _configuration.Tickets.Enabled = false;
        _discord.BeforeExistsAsync = _ => throw new AssertionException("Offline expiration must not contact Discord.");

        await _service.ExpireArchivesAsync();

        Assert.That(_archive.DeletedPaths, expires ? Is.EqualTo(new[] { path }) : Is.Empty);
        Assert.That(_store.Tickets.Single().ArchivePath, expires ? Is.Null : Is.EqualTo(path));
        Assert.That(_discord.ExistsCalls, Is.Zero);
    }

    [Test]
    public async Task OfflineExpirationHonorsHoldAndDeadlineThenExpiresReleasedArchive()
    {
        var opened = await Open("first", new Actor(1, []), "offline-held");
        var path = _archive.GetArchivePath(opened.Ticket!);
        var ticket = opened.Ticket! with
        {
            State = TicketState.Closed, Hold = true, ArchivePath = path,
            ArchiveExpiresAt = _clock.GetUtcNow().AddSeconds(-1)
        };
        _store.Replace(ticket);
        await _service.ExpireArchivesAsync();
        Assert.That(_archive.DeletedPaths, Is.Empty);
        _store.Replace(ticket with { Hold = false, ArchiveExpiresAt = _clock.GetUtcNow().AddSeconds(1) });
        await _service.ExpireArchivesAsync();
        Assert.That(_archive.DeletedPaths, Is.Empty);
        _clock.Advance(TimeSpan.FromSeconds(1));
        await _service.ExpireArchivesAsync();
        Assert.That(_archive.DeletedPaths, Is.EqualTo(new[] { path }));
    }

    [Test]
    public async Task OfflineExpirationProtectsActiveTranscriptDeliveryThenExpiresAfterRelease()
    {
        await Open("first", new Actor(1, []), "expiry-during-delivery");
        await _service.CloseAsync(ChannelOne, Support);
        _clock.Advance(TimeSpan.FromDays(91));
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var export = _service.ExportAsync(ChannelOne, Support, deliver: async (_, ct) =>
        {
            started.SetResult();
            await release.Task.WaitAsync(ct);
        });
        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
            await _service.ExpireArchivesAsync().WaitAsync(TimeSpan.FromSeconds(2));
            Assert.That(_archive.DeletedPaths, Is.Empty);
            Assert.That(_store.Tickets.Single().ArchivePath, Is.Not.Null);
            release.SetResult();
            Assert.That((await export).Success, Is.True);
            await _service.ExpireArchivesAsync();
            Assert.That(_archive.DeletedPaths, Has.Count.EqualTo(1));
            Assert.That(_store.Tickets.Single().ArchivePath, Is.Null);
        }
        finally { release.TrySetResult(); await export; }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task DisabledIntakeRetainsRequesterCloseAndStaffManagementUntilCleanup(bool guildOwner)
    {
        var requester = new Actor(1, []);
        var staff = guildOwner ? new Actor(9002, [], IsGuildOwner: true) : Support;
        _discord.CurrentActors[staff.UserId] = staff;
        await Open("first", requester, "disable-management");
        _configuration.Tickets.Enabled = false;
        // Management also has to survive a worker restart while new intake stays disabled.
        _service = new TicketService(_configuration, _store, _discord, _archive, _clock);
        var delivered = 0;

        var newTicket = await _service.OpenAsync("second", new Actor(2, []), "disable-new");
        var rename = await _service.RenameAsync(ChannelOne, staff, "Follow up");
        var export = await _service.ExportAsync(ChannelOne, staff, deliver: (_, _) =>
        {
            delivered++;
            return Task.CompletedTask;
        });
        var hold = await _service.SetHoldAsync(ChannelOne, staff, true);
        var close = await _service.CloseAsync(ChannelOne, requester);
        _clock.Advance(TimeSpan.FromDays(8));
        await _service.MaintainAsync();
        Assert.That(_discord.DeleteCalls, Is.Zero, "A staff hold still protects a closed ticket while intake is disabled.");
        var release = await _service.SetHoldAsync(ChannelOne, staff, false);
        var reopen = await _service.ReopenAsync(ChannelOne, staff);
        Assert.That((await _service.CloseAsync(ChannelOne, requester)).Success, Is.True);
        _clock.Advance(TimeSpan.FromDays(8));
        await _service.MaintainAsync();

        Assert.Multiple(() =>
        {
            Assert.That(newTicket.Success, Is.False);
            Assert.That(new[] { rename, export, hold, close, release, reopen }.All(result => result.Success), Is.True);
            Assert.That(delivered, Is.EqualTo(1));
            Assert.That(_discord.CreateCalls, Is.EqualTo(1), "Management must not enable new ticket creation.");
            Assert.That(_discord.RenameCalls, Is.EqualTo(1));
            Assert.That(_store.Tickets.Single().State, Is.EqualTo(TicketState.Deleted));
            Assert.That(_discord.DeletedChannels, Is.EqualTo(new[] { ChannelOne }));
        });
    }

    [Test]
    public async Task DisabledIntakeManagementUsesCurrentConfiguredSupportRoles()
    {
        await Open("first", new Actor(1, []), "changed-disabled-support");
        _configuration.Tickets.Enabled = false;
        _configuration.Tickets.SupportRoleIds = [901];
        var staff = new Actor(9002, [901]);
        _discord.CurrentActors[staff.UserId] = staff;

        Assert.That((await _service.CloseAsync(ChannelOne, Support)).Success, Is.False);
        Assert.That((await _service.SetHoldAsync(ChannelOne, Support, false)).Success, Is.False);
        Assert.That((await _service.ExportAsync(ChannelOne, Support)).Success, Is.False);
        Assert.That((await _service.RenameAsync(ChannelOne, staff, "Current staff")).Success, Is.True);
        Assert.That((await _service.ExportAsync(ChannelOne, staff, deliver: (_, _) => Task.CompletedTask)).Success, Is.True);
        Assert.That((await _service.SetHoldAsync(ChannelOne, staff, true)).Success, Is.True);
        Assert.That((await _service.CloseAsync(ChannelOne, staff)).Success, Is.True);
        Assert.That((await _service.SetHoldAsync(ChannelOne, staff, false)).Success, Is.True);
        Assert.That((await _service.ReopenAsync(ChannelOne, staff)).Success, Is.True);
        Assert.That(_store.Tickets.Single().State, Is.EqualTo(TicketState.Open));
    }
    [Test]
    public async Task DisabledTicketingRejectsNewTicketsButCleansExistingClosedTicket()
    {
        await Open("first", new Actor(1, []), "disable-existing");
        await _service.CloseAsync(ChannelOne, Support);
        _clock.Advance(TimeSpan.FromDays(8));
        _configuration.Tickets.Enabled = false;

        Assert.That((await _service.OpenAsync("second", new Actor(2, []), "disable-new")).Success, Is.False);
        await _service.MaintainAsync();

        Assert.That(_discord.CreateCalls, Is.EqualTo(1));
        Assert.That(_store.Tickets.Single().State, Is.EqualTo(TicketState.Deleted));
        Assert.That(_discord.DeletedChannels, Is.EqualTo(new[] { ChannelOne }));
    }

    [Test]
    public async Task DisabledOfflineExpirationDeletesExpiredFilesAndPreservesHeldFilesWithoutDiscord()
    {
        var root = Path.Combine(Path.GetTempPath(), "swlor-offline-retention", Guid.NewGuid().ToString("N"));
        try
        {
            _configuration.Tickets.ArchiveDirectory = root;
            _configuration.Tickets.CopyAttachments = false;
            using var http = new HttpClient();
            var files = new FileTranscriptArchive(_configuration, http);
            var expired = (await Open("first", new Actor(1, []), "files-expired")).Ticket!;
            var held = (await Open("second", new Actor(2, []), "files-held")).Ticket!;
            expired = expired with { ArchivePath = files.GetArchivePath(expired) };
            held = held with { ArchivePath = files.GetArchivePath(held) };
            _store.Replace(expired);
            _store.Replace(held);
            var expiredPath = await files.ExportAsync(expired, new TranscriptSnapshot([], null), default);
            var heldPath = await files.ExportAsync(held, new TranscriptSnapshot([], null), default);
            _store.Replace(expired with { State = TicketState.Closed, ArchiveSnapshotPath = expiredPath, ArchiveComplete = true, ArchiveExpiresAt = _clock.GetUtcNow() });
            _store.Replace(held with { State = TicketState.Deleting, Hold = true, ArchiveSnapshotPath = heldPath, ArchiveComplete = true, ArchiveExpiresAt = _clock.GetUtcNow() });
            _configuration.Tickets.Enabled = false;
            _service = new TicketService(_configuration, _store, _discord, files, _clock);
            _discord.BeforeExistsAsync = _ => throw new AssertionException("Offline expiration must not contact Discord.");

            await _service.ExpireArchivesAsync();

            Assert.That(Directory.Exists(expiredPath), Is.False);
            Assert.That(File.Exists(Path.Combine(heldPath, "transcript.html")), Is.True);
            Assert.That(_discord.ExistsCalls, Is.Zero);
            Assert.That(_store.Tickets.Single(t => t.Id == expired.Id).ArchivePath, Is.Null);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
    [TestCase(false, "html")]
    [TestCase(false, "seal")]
    [TestCase(false, "missing-json")]
    [TestCase(true, "html")]
    [TestCase(true, "seal")]
    [TestCase(true, "missing-json")]
    public async Task InvalidSelectedArchiveDoesNotBlockFreshExportOrDueCleanup(bool cleanup, string corruption)
    {
        var root = Path.Combine(Path.GetTempPath(), "swlor-prune-recovery", Guid.NewGuid().ToString("N"));
        try
        {
            _configuration.Tickets.ArchiveDirectory = root;
            _configuration.Tickets.CopyAttachments = false;
            using var http = new HttpClient();
            var files = new FileTranscriptArchive(_configuration, http);
            _service = new TicketService(_configuration, _store, _discord, files, _clock);
            await Open("first", new Actor(1, []), "corrupt-selected-export");
            _discord.Snapshot = new([new(10, 1, "member", "previous evidence", _clock.GetUtcNow(), [])], 10);
            await _service.ExportAsync(ChannelOne, Support);
            var previous = _store.Tickets.Single();
            var selected = previous.ArchiveSnapshotPath!;
            if (corruption == "html") await File.AppendAllTextAsync(Path.Combine(selected, "transcript.html"), "changed");
            else if (corruption == "seal") await File.WriteAllTextAsync(Path.Combine(selected, "attachment-manifest.sha256"), new string('0', 64));
            else File.Delete(Path.Combine(selected, "transcript.json"));
            var priorHtml = await File.ReadAllBytesAsync(Path.Combine(selected, "transcript.html"));
            var reads = 0;
            _discord.BeforeTranscriptAsync = (_, _) =>
            {
                if (++reads == 1)
                {
                    Assert.That(Directory.Exists(selected), Is.True, "A failed validation must not prune the selected generation.");
                    Assert.That(File.ReadAllBytes(Path.Combine(selected, "transcript.html")), Is.EqualTo(priorHtml));
                    Assert.That(_store.Tickets.Single().ArchiveSnapshotPath, Is.EqualTo(selected));
                }
                return Task.CompletedTask;
            };
            _discord.Snapshot = new([new(11, 1, "member", "replacement evidence", _clock.GetUtcNow(), [])], 11);
            if (cleanup)
            {
                await _service.CloseAsync(ChannelOne, Support);
                _clock.Advance(TimeSpan.FromDays(8));
                await _service.MaintainAsync();
                Assert.That(_store.Tickets.Single().State, Is.EqualTo(TicketState.Deleted));
                Assert.That(_discord.DeleteCalls, Is.EqualTo(1));
                Assert.That(reads, Is.EqualTo(2));
            }
            else
            {
                Assert.That((await _service.ExportAsync(ChannelOne, Support)).Success, Is.True);
                Assert.That(reads, Is.EqualTo(1));
                Assert.That(_discord.DeleteCalls, Is.Zero);
            }
            var current = _store.Tickets.Single();
            Assert.That(current.ArchiveComplete, Is.True);
            Assert.That(current.ArchiveSnapshotPath, Is.Not.EqualTo(selected));
            Assert.That(await File.ReadAllTextAsync(Path.Combine(current.ArchiveSnapshotPath!, "transcript.html")), Does.Contain("replacement evidence"));
            Assert.That(Directory.Exists(selected), Is.False, "Only a successfully selected replacement permits reclamation.");
            await files.PruneSnapshotsAsync(current, default);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task PreExportPruningPreservesCallerAndInactivityCancellationAndReleasesGuard(bool cleanup, bool inactivity)
    {
        await Open("first", new Actor(1, []), "prune-cancellation");
        if (cleanup)
        {
            await _service.CloseAsync(ChannelOne, Support);
            _clock.Advance(TimeSpan.FromDays(8));
        }
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reads = 0;
        _discord.BeforeTranscriptAsync = (_, _) => { reads++; return Task.CompletedTask; };
        _archive.BeforePruneAsync = async (_, progress, token) =>
        {
            progress();
            started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
        };
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var operation = cleanup ? _service.MaintainAsync(cancellation.Token) :
            _service.ExportAsync(ChannelOne, Support, cancellation.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        if (inactivity) _clock.Advance(TimeSpan.FromMinutes(4));
        else cancellation.Cancel();
        if (cleanup && inactivity) await operation.WaitAsync(TimeSpan.FromSeconds(2));
        else Assert.CatchAsync<OperationCanceledException>(() => operation.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.That(_archive.ExportCalls, Is.Zero);
        Assert.That(reads, Is.Zero);
        Assert.That(_archive.PruneCalls, Is.EqualTo(1));
        Assert.That(_discord.DeleteCalls, Is.Zero);
        Assert.That(_store.Tickets.Single().ArchiveComplete, Is.False);
        _archive.BeforePruneAsync = null;
        if (cleanup)
        {
            await _service.MaintainAsync();
            Assert.That(_discord.DeleteCalls, Is.EqualTo(1));
        }
        else Assert.That((await _service.ExportAsync(ChannelOne, Support)).Success, Is.True);
    }
    [TestCase("scan")]
    [TestCase("ownership")]
    [TestCase("storage")]
    [TestCase("completion")]
    public async Task FailedRefreshKeepsTheCompleteSavedSnapshotUsable(string failure)
    {
        await Open("first", new Actor(1, []), "refresh-failure");
        await _service.ExportAsync(ChannelOne, Support);
        var previous = _store.Tickets.Single();
        if (failure == "scan")
            _discord.BeforeTranscriptAsync = (_, _) => throw new InvalidOperationException("Discord pagination failed.");
        else if (failure == "ownership") _store.FailNextSaveAction = "archive-pending";
        else if (failure == "storage") _archive.FailExports = true;
        else _store.FailNextSaveAction = "exported";

        Assert.ThrowsAsync<InvalidOperationException>(() => _service.ExportAsync(ChannelOne, Support));

        var retained = _store.Tickets.Single();
        Assert.That(retained.ArchivePath, Is.EqualTo(previous.ArchivePath));
        Assert.That(retained.ArchiveSnapshotPath, Is.EqualTo(previous.ArchiveSnapshotPath));
        Assert.That(retained.ArchiveComplete, Is.True);
        var delivered = false;
        var result = await _service.ExportAsync(ChannelOne, Support, useSavedArchive: true,
            deliver: (ticket, _) =>
            {
                Assert.That(ticket.ArchiveSnapshotPath, Is.EqualTo(previous.ArchiveSnapshotPath));
                delivered = true;
                return Task.CompletedTask;
            });
        Assert.That(result.Success, Is.True);
        Assert.That(delivered, Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task StalledRefreshPreservesSavedSnapshotAndProtectsItFromConcurrentExpiration(bool storage)
    {
        await Open("first", new Actor(1, []), "refresh-timeout");
        await _service.ExportAsync(ChannelOne, Support);
        await _service.CloseAsync(ChannelOne, Support);
        _clock.Advance(TimeSpan.FromDays(91));
        var previous = _store.Tickets.Single();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task Stall(Ticket ticket, Action progress, CancellationToken token)
        {
            started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
        }
        if (storage) _archive.BeforeProgressExportAsync = Stall;
        else _discord.BeforeProgressTranscriptAsync = Stall;
        var refresh = _service.ExportAsync(ChannelOne, Support);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await _service.ExpireArchivesAsync().WaitAsync(TimeSpan.FromSeconds(2));
        await _service.MaintainAsync().WaitAsync(TimeSpan.FromSeconds(2));
        Assert.That(_archive.DeletedPaths, Is.Empty);
        Assert.That(_discord.DeleteCalls, Is.Zero);
        Assert.That(_store.Tickets.Single().ArchiveComplete, Is.True);
        _clock.Advance(TimeSpan.FromMinutes(4));
        Assert.CatchAsync<OperationCanceledException>(() => refresh.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.That(_store.Tickets.Single().ArchiveSnapshotPath, Is.EqualTo(previous.ArchiveSnapshotPath));
        Assert.That((await _service.ExportAsync(ChannelOne, Support, useSavedArchive: true)).Success, Is.True);
        await _service.ExpireArchivesAsync();
        Assert.That(_archive.DeletedPaths, Is.EqualTo(new[] { previous.ArchivePath }));
        Assert.That(_store.Tickets.Single().ArchiveSnapshotPath, Is.Null);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task InterruptedRefreshSaveKeepsPriorFilesAcrossRestartThenSuccessfulRefreshPrunesThem(bool legacy)
    {
        var root = Path.Combine(Path.GetTempPath(), "swlor-refresh-tests", Guid.NewGuid().ToString("N"));
        try
        {
            _configuration.Tickets.ArchiveDirectory = root;
            _configuration.Tickets.CopyAttachments = false;
            using var http = new HttpClient();
            var files = new FileTranscriptArchive(_configuration, http);
            _service = new TicketService(_configuration, _store, _discord, files, _clock);
            var ticket = (await Open("first", new Actor(1, []), "refresh-restart")).Ticket!;
            string previousDirectory;
            if (legacy)
            {
                previousDirectory = files.GetArchivePath(ticket);
                Directory.CreateDirectory(previousDirectory);
                await File.WriteAllTextAsync(Path.Combine(previousDirectory, "transcript.json"), "legacy JSON");
                await File.WriteAllTextAsync(Path.Combine(previousDirectory, "transcript.html"), "legacy HTML");
                _store.Replace(ticket with { ArchivePath = previousDirectory, ArchiveComplete = true });
            }
            else
            {
                await _service.ExportAsync(ChannelOne, Support);
                previousDirectory = _store.Tickets.Single().ArchiveSnapshotPath!;
            }
            var previous = _store.Tickets.Single();
            var originalJson = await File.ReadAllBytesAsync(Path.Combine(previousDirectory, "transcript.json"));
            var originalHtml = await File.ReadAllBytesAsync(Path.Combine(previousDirectory, "transcript.html"));
            _discord.Snapshot = new([new(10, 1, "member", "replacement", _clock.GetUtcNow(), [])], 10);
            _store.FailNextSaveAction = "exported";
            Assert.ThrowsAsync<InvalidOperationException>(() => _service.ExportAsync(ChannelOne, Support));
            Assert.That(_store.Tickets.Single(), Is.EqualTo(previous));
            Assert.That(await File.ReadAllBytesAsync(Path.Combine(previousDirectory, "transcript.json")), Is.EqualTo(originalJson));
            Assert.That(await File.ReadAllBytesAsync(Path.Combine(previousDirectory, "transcript.html")), Is.EqualTo(originalHtml));
            Assert.That(Directory.GetFiles(previous.ArchivePath!, "transcript.html", SearchOption.AllDirectories), Has.Length.EqualTo(2),
                "The unpublished complete pair remains inside the durably owned root until retry or expiration.");
            _service = new TicketService(_configuration, _store, _discord, files, _clock);
            Assert.That((await _service.ExportAsync(ChannelOne, Support, useSavedArchive: true,
                deliver: async (saved, token) =>
                {
                    var selected = saved.ArchiveSnapshotPath ?? saved.ArchivePath!;
                    Assert.That(selected, Is.EqualTo(previousDirectory));
                    Assert.That(await File.ReadAllBytesAsync(Path.Combine(selected, "transcript.html"), token), Is.EqualTo(originalHtml));
                })).Success, Is.True);
            var refreshed = await _service.ExportAsync(ChannelOne, Support);
            Assert.That(refreshed.Success, Is.True);
            Assert.That(refreshed.Ticket!.ArchivePath, Is.EqualTo(previous.ArchivePath));
            Assert.That(refreshed.Ticket.ArchiveSnapshotPath, Is.Not.EqualTo(previousDirectory));
            Assert.That(refreshed.Ticket.ArchiveComplete, Is.True);
            var selectedPath = refreshed.Ticket.ArchiveSnapshotPath!;
            Assert.That(await File.ReadAllTextAsync(Path.Combine(selectedPath, "transcript.html")), Does.Contain("replacement"));
            Assert.That(Directory.GetFiles(previous.ArchivePath!, "transcript.html", SearchOption.AllDirectories),
                Is.EqualTo(new[] { Path.Combine(selectedPath, "transcript.html") }));
            Assert.That(File.Exists(Path.Combine(previousDirectory, "transcript.html")), Is.False);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public async Task SavedTranscriptDownloadPreservesTheArchiveAndRefreshesLiveAuthorizationAndOwnership()
    {
        await Open("first", new Actor(1, []), "saved-download");
        await _service.ExportAsync(ChannelOne, Support);
        await _service.CloseAsync(ChannelOne, Support);
        await _service.SetHoldAsync(ChannelOne, Support, true);
        var retained = _store.Tickets.Single();
        var existsBefore = _discord.ExistsCalls;
        _discord.ActorLookups.Clear();
        _discord.BeforeTranscriptAsync = (_, _) => throw new AssertionException("A saved download must not rescan Discord history.");
        _archive.BeforeExportAsync = (_, _) => throw new AssertionException("A saved download must not publish or rewrite the archive.");
        var delivered = 0;

        var result = await _service.ExportAsync(ChannelOne, Support, deliver: (ticket, token) =>
        {
            token.ThrowIfCancellationRequested();
            Assert.That(ticket, Is.EqualTo(retained));
            delivered++;
            return Task.CompletedTask;
        }, useSavedArchive: true);

        Assert.That(result.Success, Is.True);
        Assert.That(delivered, Is.EqualTo(1));
        Assert.That(_store.Tickets.Single(), Is.EqualTo(retained), "Saved downloads preserve holds, deadlines and completion state.");
        Assert.That(_discord.ActorLookups, Is.EqualTo(new[] { Support.UserId, Support.UserId }));
        Assert.That(_discord.ExistsCalls - existsBefore, Is.EqualTo(2));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task SavedDownloadRejectsMissingOrIncompleteArchiveWithoutCapturing(bool incomplete)
    {
        await Open("first", new Actor(1, []), "saved-not-ready");
        if (incomplete)
        {
            var ticket = _store.Tickets.Single();
            _store.Replace(ticket with { ArchivePath = _archive.GetArchivePath(ticket), ArchiveComplete = false });
        }
        var retained = _store.Tickets.Single();
        _discord.BeforeTranscriptAsync = (_, _) => throw new AssertionException("Missing saved evidence must not trigger a fresh export.");
        var delivered = 0;
        var result = await _service.ExportAsync(ChannelOne, Support,
            deliver: (_, _) => { delivered++; return Task.CompletedTask; }, useSavedArchive: true);
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("no complete saved transcript"));
        Assert.That(delivered, Is.Zero);
        Assert.That(_store.Tickets.Single(), Is.EqualTo(retained));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task SavedDownloadRejectsSupportLossBeforePreparationOrDeliveryAndReleasesGuard(bool beforePreparation)
    {
        await Open("first", new Actor(1, []), "saved-support-loss");
        await _service.ExportAsync(ChannelOne, Support);
        if (beforePreparation) _discord.CurrentActors[Support.UserId] = new Actor(Support.UserId, []);
        else _discord.BeforeExistsAsync = _ =>
        {
            _discord.CurrentActors[Support.UserId] = new Actor(Support.UserId, []);
            return Task.CompletedTask;
        };
        var delivered = 0;
        var denied = await _service.ExportAsync(ChannelOne, Support,
            deliver: (_, _) => { delivered++; return Task.CompletedTask; }, useSavedArchive: true);
        Assert.That(denied.Success, Is.False);
        Assert.That(delivered, Is.Zero);
        _discord.BeforeExistsAsync = null;
        _discord.CurrentActors[Support.UserId] = Support;
        Assert.That((await _service.ExportAsync(ChannelOne, Support,
            deliver: (_, _) => { delivered++; return Task.CompletedTask; }, useSavedArchive: true)).Success, Is.True);
        Assert.That(delivered, Is.EqualTo(1));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task SavedDownloadRequiresManagedChannelIdentityBeforePreparationAndDelivery(bool beforePreparation)
    {
        await Open("first", new Actor(1, []), "saved-channel-identity");
        await _service.ExportAsync(ChannelOne, Support);
        var lookups = 0;
        _discord.BeforeExistsAsync = _ =>
        {
            if (beforePreparation || ++lookups == 2)
                throw new InvalidOperationException("The ticket channel no longer has its managed identity.");
            return Task.CompletedTask;
        };
        var delivered = 0;
        Assert.ThrowsAsync<InvalidOperationException>(() => _service.ExportAsync(ChannelOne, Support,
            deliver: (_, _) => { delivered++; return Task.CompletedTask; }, useSavedArchive: true));
        Assert.That(delivered, Is.Zero);
        _discord.BeforeExistsAsync = null;
        Assert.That((await _service.ExportAsync(ChannelOne, Support, useSavedArchive: true)).Success, Is.True);
    }

    [Test]
    public async Task SavedDownloadKeepsFilesProtectedDuringDeliveryAndRejectsOverlappingExports()
    {
        await Open("first", new Actor(1, []), "saved-protected");
        await _service.ExportAsync(ChannelOne, Support);
        await _service.CloseAsync(ChannelOne, Support);
        _clock.Advance(TimeSpan.FromDays(8));
        var result = await _service.ExportAsync(ChannelOne, Support, deliver: async (_, _) =>
        {
            Assert.That((await _service.ExportAsync(ChannelOne, Support, useSavedArchive: true)).Success, Is.False);
            await _service.MaintainAsync();
            Assert.That(_discord.DeleteCalls, Is.Zero);
            Assert.That(_store.Tickets.Single().ArchiveComplete, Is.True);
        }, useSavedArchive: true);
        Assert.That(result.Success, Is.True);
        await _service.MaintainAsync();
        Assert.That(_discord.DeleteCalls, Is.EqualTo(1));
    }

    [Test]
    public async Task FreshSavedInteractionCanDownloadArchiveAfterLongExportOutlivesItsOriginalInteraction()
    {
        await Open("first", new Actor(1, []), "long-interaction-expiry");
        _discord.BeforeProgressTranscriptAsync = async (_, progress, token) =>
        {
            for (var page = 0; page < 6; page++)
            {
                _clock.Advance(TimeSpan.FromMinutes(3));
                token.ThrowIfCancellationRequested();
                progress();
                await Task.Yield();
            }
        };
        Assert.ThrowsAsync<InvalidOperationException>(() => _service.ExportAsync(ChannelOne, Support,
            deliver: (_, _) => throw new InvalidOperationException("The original interaction token expired.")));
        Assert.That(_store.Tickets.Single().ArchiveComplete, Is.True);
        _discord.BeforeProgressTranscriptAsync = null;
        _discord.BeforeTranscriptAsync = (_, _) => throw new AssertionException("The fresh saved interaction must not repeat the slow scan.");
        _archive.BeforeExportAsync = (_, _) => throw new AssertionException("The complete archive must be reused.");
        var delivered = 0;
        var saved = await _service.ExportAsync(ChannelOne, Support,
            deliver: (_, _) => { delivered++; return Task.CompletedTask; }, useSavedArchive: true);
        Assert.That(saved.Success, Is.True);
        Assert.That(delivered, Is.EqualTo(1));
    }
    [TestCase(false)]
    [TestCase(true)]
    public async Task ManualExportProgressCanExceedTwelveMinutes(bool slowArchive)
    {
        await Open("first", new Actor(1, []), "large-manual-export");
        var before = _clock.GetUtcNow();
        var reports = 0;
        async Task Work(Ticket ticket, Action progress, CancellationToken ct)
        {
            for (var page = 0; page < 5; page++)
            {
                _clock.Advance(TimeSpan.FromMinutes(3));
                ct.ThrowIfCancellationRequested();
                progress();
                reports++;
                await Task.Yield();
            }
        }
        if (slowArchive) _archive.BeforeProgressExportAsync = Work;
        else _discord.BeforeProgressTranscriptAsync = Work;

        var result = await _service.ExportAsync(ChannelOne, Support);

        Assert.That(result.Success, Is.True);
        Assert.That(reports, Is.EqualTo(5));
        Assert.That(_clock.GetUtcNow() - before, Is.GreaterThan(TimeSpan.FromMinutes(12)));
        Assert.That(_store.Tickets.Single().ArchiveComplete, Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ManualExportStallsCancelWithoutMarkingCompleteAndReleaseGuard(bool blockArchive)
    {
        await Open("first", new Actor(1, []), "stalled-manual-export");
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task Work(Ticket ticket, Action progress, CancellationToken ct)
        {
            _clock.Advance(TimeSpan.FromMinutes(3));
            progress();
            started.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
        }
        if (blockArchive) _archive.BeforeProgressExportAsync = Work;
        else _discord.BeforeProgressTranscriptAsync = Work;
        var export = _service.ExportAsync(ChannelOne, Support);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        _clock.Advance(TimeSpan.FromMinutes(4));
        Assert.CatchAsync<OperationCanceledException>(async () => await export.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.That(_store.Tickets.Single().ArchiveComplete, Is.False);
        _archive.BeforeProgressExportAsync = null;
        _discord.BeforeProgressTranscriptAsync = null;
        Assert.That((await _service.ExportAsync(ChannelOne, Support)).Success, Is.True);
    }

    [Test]
    public async Task ReadinessReconciliationCanProgressAcrossTicketsBeyondTwelveMinutes()
    {
        for (var index = 0; index < 4; index++)
            await Open(index < 2 ? "first" : "second", new Actor((ulong)(index + 1), []), "large-readiness-" + index);
        var before = _clock.GetUtcNow();
        _discord.BeforeReconcileAsync = (_, token) =>
        {
            _clock.Advance(TimeSpan.FromMinutes(3));
            token.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        };

        await _service.ReconcileRetainedPermissionsAsync();

        Assert.That(_clock.GetUtcNow() - before, Is.EqualTo(TimeSpan.FromMinutes(12)));
        Assert.That(_discord.ReconciledTickets, Has.Count.EqualTo(4));
    }

    [Test]
    public async Task ReadinessReconciliationStallTimesOutAndReleasesDatabaseLock()
    {
        await Open("first", new Actor(1, []), "stalled-readiness");
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _discord.BeforeReconcileAsync = async (_, token) =>
        {
            started.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
        };
        var reconciliation = _service.ReconcileRetainedPermissionsAsync();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        _clock.Advance(TimeSpan.FromMinutes(4));
        Assert.CatchAsync<OperationCanceledException>(async () => await reconciliation.WaitAsync(TimeSpan.FromSeconds(2)));
        _discord.BeforeReconcileAsync = null;
        await _service.ReconcileRetainedPermissionsAsync().WaitAsync(TimeSpan.FromSeconds(2));
        Assert.That(_discord.ReconciledTickets, Has.Count.EqualTo(1));
    }
    [TestCase(false)]
    [TestCase(true)]
    public async Task ForwardProgressAllowsLargeCleanupAndVerificationBeyondTwelveMinutes(bool slowArchive)
    {
        await Open("first", new Actor(1, []), "large-progressing");
        await Open("second", new Actor(2, []), "later-missing");
        await _service.CloseAsync(ChannelOne, Support);
        _clock.Advance(TimeSpan.FromDays(8));
        _discord.MissingChannels.Add(ChannelOne + 1);
        var before = _clock.GetUtcNow();
        var progressReports = 0;
        async Task AdvanceWithProgress(Ticket ticket, Action progress, CancellationToken ct)
        {
            if (ticket.ChannelId != ChannelOne) return;
            for (var page = 0; page < 4; page++)
            {
                _clock.Advance(TimeSpan.FromMinutes(3));
                ct.ThrowIfCancellationRequested();
                progress();
                progressReports++;
                await Task.Yield();
            }
        }
        if (slowArchive) _archive.BeforeProgressExportAsync = AdvanceWithProgress;
        else _discord.BeforeProgressTranscriptAsync = AdvanceWithProgress;

        await _service.MaintainAsync();

        Assert.That(_clock.GetUtcNow() - before, Is.GreaterThanOrEqualTo(TimeSpan.FromMinutes(12)));
        Assert.That(progressReports, Is.EqualTo(slowArchive ? 4 : 8), "Both initial and verification scans must report progress.");
        Assert.That(_store.Tickets.All(ticket => ticket.State == TicketState.Deleted), Is.True);
        Assert.That(_store.Tickets.Single(ticket => ticket.ChannelId == ChannelOne).LastError, Is.Null);
        Assert.That(_discord.DeletedChannels, Is.EqualTo(new[] { ChannelOne }));
    }

    [Test]
    public async Task ProgressThenStallStillTimesOutAndAllowsLaterCleanup()
    {
        await Open("first", new Actor(1, []), "progress-stalls");
        await Open("second", new Actor(2, []), "later-cleanup");
        await _service.CloseAsync(ChannelOne, Support);
        await _service.CloseAsync(ChannelOne + 1, Support);
        _clock.Advance(TimeSpan.FromDays(8));
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _discord.BeforeProgressTranscriptAsync = async (ticket, progress, ct) =>
        {
            if (ticket.ChannelId != ChannelOne) return;
            _clock.Advance(TimeSpan.FromMinutes(3));
            progress();
            started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
        };
        using var safety = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var cleanup = _service.MaintainAsync(safety.Token);
        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
            _clock.Advance(TimeSpan.FromMinutes(4) - TimeSpan.FromSeconds(1));
            Assert.That(cleanup.IsCompleted, Is.False);
            _clock.Advance(TimeSpan.FromSeconds(1));
            await cleanup.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.That(_store.Tickets.Single(ticket => ticket.ChannelId == ChannelOne).State, Is.EqualTo(TicketState.Deleting));
            Assert.That(_store.Tickets.Single(ticket => ticket.ChannelId == ChannelOne).LastError, Is.Not.Null);
            Assert.That(_discord.DeletedChannels, Is.EqualTo(new[] { ChannelOne + 1 }));
            _discord.BeforeProgressTranscriptAsync = null;
            await _service.MaintainAsync();
            Assert.That(_discord.DeletedChannels, Is.EquivalentTo(new[] { ChannelOne, ChannelOne + 1 }));
        }
        finally { safety.Cancel(); try { await cleanup; } catch (OperationCanceledException) { } }
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task ChannelDiscoveryAndCreationReleaseGuildLockAndMergeConcurrentHold(bool maintenance, bool creating)
    {
        await Open("second", new Actor(2, []), "discovery-unrelated");
        Ticket pending;
        await using (var reserve = await _store.LockAsync(default))
            pending = await reserve.ReserveAsync("first", 1, "delayed-channel", _clock.GetUtcNow(), default);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task DelayRemote(CancellationToken ct)
        {
            _clock.Advance(TimeSpan.FromSeconds(90));
            started.TrySetResult();
            await release.Task.WaitAsync(ct);
        }
        if (creating) _discord.BeforeCreateAsync = (ticket, ct) => ticket.Id == pending.Id ? DelayRemote(ct) : Task.CompletedTask;
        else _discord.BeforeFindManagedAsync = (id, ct) => id == pending.Id ? DelayRemote(ct) : Task.CompletedTask;
        _discord.BeforeProgressOpenAsync = (ticket, _, _) =>
        {
            if (ticket.Id == pending.Id) Assert.That(ticket.Hold, Is.True, "Opening must use the fresh record read after discovery/creation.");
            return Task.CompletedTask;
        };
        using var safety = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        Task opening = maintenance ? _service.MaintainAsync(safety.Token)
            : _service.OpenAsync("first", new Actor(1, []), "delayed-channel", safety.Token);
        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.That(_clock.GetUtcNow() - pending.CreatedAt, Is.GreaterThan(TimeSpan.FromSeconds(60)));
            Assert.That((await _service.CloseAsync(ChannelOne, Support).WaitAsync(TimeSpan.FromSeconds(2))).Success, Is.True);
            Assert.That((await _service.RenameAsync(ChannelOne, Support, "during-discovery").WaitAsync(TimeSpan.FromSeconds(2))).Success, Is.True);
            Assert.That((await _service.ReopenAsync(ChannelOne, Support).WaitAsync(TimeSpan.FromSeconds(2))).Success, Is.True);
            Assert.That((await _service.SetHoldAsync(ChannelOne, Support, true).WaitAsync(TimeSpan.FromSeconds(2))).Success, Is.True);
            Assert.That((await _service.OpenAsync("second", new Actor(3, []), "discovery-another").WaitAsync(TimeSpan.FromSeconds(2))).Success, Is.True);
            await using (var update = await _store.LockAsync(default).WaitAsync(TimeSpan.FromSeconds(2)))
            {
                var durable = await update.GetTicketAsync(pending.Id, default);
                Assert.That(durable!.ChannelId, Is.Null, "The remote channel has not been bound yet.");
                await update.SaveAsync(durable with { Hold = true }, "hold-set", Support.UserId, default);
            }
            var findCalls = _discord.FindManagedCalls;
            var createCalls = _discord.CreateCalls;
            var openCalls = _discord.OpenCalls;
            var duplicate = await _service.OpenAsync("first", new Actor(1, []), "delayed-channel").WaitAsync(TimeSpan.FromSeconds(2));
            var samePanel = await _service.OpenAsync("first", new Actor(1, []), "delayed-channel-click").WaitAsync(TimeSpan.FromSeconds(2));
            Assert.That(duplicate.Success, Is.False);
            Assert.That(samePanel.Success, Is.False);
            Assert.That(duplicate.Message, Does.Contain("already being opened"));
            await _service.MaintainAsync().WaitAsync(TimeSpan.FromSeconds(2));
            Assert.That(_discord.FindManagedCalls, Is.EqualTo(findCalls));
            Assert.That(_discord.CreateCalls, Is.EqualTo(createCalls));
            Assert.That(_discord.OpenCalls, Is.EqualTo(openCalls));
            release.SetResult();
            await opening.WaitAsync(TimeSpan.FromSeconds(2));
            var completed = _store.Tickets.Single(ticket => ticket.Id == pending.Id);
            Assert.That(completed.State, Is.EqualTo(TicketState.Open));
            Assert.That(completed.Hold, Is.True);
            Assert.That(completed.ChannelId, Is.Not.Null);
            Assert.That(_discord.CreateCalls, Is.EqualTo(3));
        }
        finally
        {
            release.TrySetResult();
            safety.Cancel();
            try { await opening; } catch (OperationCanceledException) { }
        }
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task CancelledChannelDiscoveryOrCreationReleasesGuardAndRecoversUnboundTicket(bool createdRemotely, bool restart)
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task WaitForCancellation(CancellationToken ct)
        {
            started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
        }
        if (createdRemotely) _discord.AfterCreateAsync = (_, ct) => WaitForCancellation(ct);
        else _discord.BeforeFindManagedAsync = (_, ct) => WaitForCancellation(ct);
        using var cancellation = new CancellationTokenSource();
        var opening = _service.OpenAsync("first", new Actor(1, []), "cancel-before-binding", cancellation.Token);
        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
            await using (var update = await _store.LockAsync(default).WaitAsync(TimeSpan.FromSeconds(2)))
            {
                var pending = (await update.GetTicketsAsync(default)).Single();
                await update.SaveAsync(pending with { Hold = true }, "hold-set", Support.UserId, default);
            }
            cancellation.Cancel();
            Assert.That(async () => await opening.WaitAsync(TimeSpan.FromSeconds(2)), Throws.InstanceOf<OperationCanceledException>());
            Assert.That(_store.Tickets.Single().State, Is.EqualTo(TicketState.Creating));
            Assert.That(_store.Tickets.Single().ChannelId, Is.Null);
            Assert.That(_store.Tickets.Single().Hold, Is.True);
            Assert.That(_store.Audits, Does.Not.Contain("channel-bound"));
            _discord.BeforeFindManagedAsync = null;
            _discord.AfterCreateAsync = null;
            if (restart)
                await new TicketService(_configuration, _store, _discord, _archive, _clock).MaintainAsync();
            else Assert.That((await _service.OpenAsync("first", new Actor(1, []), "cancel-before-binding")).Success, Is.True);
            Assert.That(_store.Tickets.Single().State, Is.EqualTo(TicketState.Open));
            Assert.That(_store.Tickets.Single().Hold, Is.True);
            Assert.That(_store.Tickets.Single().ChannelId, Is.EqualTo(ChannelOne));
            Assert.That(_discord.CreateCalls, Is.EqualTo(1), "An ambiguously created remote channel must be discovered and reused.");
            Assert.That(_store.Audits.Count(action => action == "channel-bound"), Is.EqualTo(1));
        }
        finally { cancellation.Cancel(); try { await opening; } catch (OperationCanceledException) { } }
    }

    [TestCase("discovery")]
    [TestCase("creation")]
    [TestCase("opening")]
    public async Task PersistentCreationErrorsDoNotRepeatBindingOrFailureAuditsAndSuccessfulRecoveryClearsError(string phase)
    {
        Task Fail() => throw new InvalidOperationException("Persistent remote creation failure.");
        if (phase == "discovery") _discord.BeforeFindManagedAsync = (_, _) => Fail();
        else if (phase == "creation") _discord.BeforeCreateAsync = (_, _) => Fail();
        else _discord.BeforeProgressOpenAsync = (_, _, _) => Fail();
        var requester = new Actor(1, []);
        Assert.That((await Open("first", requester, "persistent-creation")).Success, Is.False);
        var initialAuditCount = _store.Audits.Count;
        var initialBindingCount = _store.Audits.Count(action => action == "channel-bound");
        var initialError = _store.Tickets.Single().LastError;
        Assert.That(initialError, Is.Not.Null);
        Assert.That(_store.Audits.Count(action => action == "creation-failed"), Is.EqualTo(1));
        await _service.MaintainAsync();
        await _service.MaintainAsync();
        Assert.That((await _service.OpenAsync("first", requester, "persistent-creation")).Success, Is.False);
        Assert.That(_store.Audits.Count, Is.EqualTo(initialAuditCount), "Unchanged periodic and manual failures must not grow the audit.");
        Assert.That(_store.Tickets.Single().LastError, Is.EqualTo(initialError));
        _discord.BeforeFindManagedAsync = null;
        _discord.BeforeCreateAsync = null;
        _discord.BeforeProgressOpenAsync = null;
        Assert.That((await _service.OpenAsync("first", requester, "persistent-creation")).Success, Is.True);
        var recovered = _store.Tickets.Single();
        Assert.That(recovered.LastError, Is.Null);
        Assert.That(recovered.State, Is.EqualTo(TicketState.Open));
        Assert.That(_store.Audits.Count(action => action == "channel-bound"), Is.EqualTo(1));
        Assert.That(initialBindingCount, Is.EqualTo(phase == "opening" ? 1 : 0));
        _store.Replace(recovered with { State = TicketState.Creating });
        _discord.BeforeProgressOpenAsync = (_, _, _) => Fail();
        Assert.That((await _service.OpenAsync("first", requester, "persistent-creation")).Success, Is.False);
        Assert.That(_store.Audits.Count(action => action == "creation-failed"), Is.EqualTo(2), "A new failure after successful recovery must be recorded.");
        Assert.That(_store.Audits.Count(action => action == "channel-bound"), Is.EqualTo(1));
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task OpeningHistoryReleasesGuildLockAndPreservesConcurrentHold(bool maintenance, bool failCompletion)
    {
        var requester = new Actor(1, []);
        _discord.FailNextOpen = true;
        Assert.That((await Open("first", requester, "unlocked-opening")).Success, Is.False);
        await Open("second", new Actor(2, []), "unrelated-ticket");
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _discord.BeforeProgressOpenAsync = async (ticket, progress, ct) =>
        {
            if (ticket.ChannelId != ChannelOne) return;
            for (var page = 0; page < 12; page++)
            {
                _clock.Advance(TimeSpan.FromMinutes(1));
                progress();
                ct.ThrowIfCancellationRequested();
            }
            started.TrySetResult();
            await release.Task.WaitAsync(ct);
        };
        using var safety = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        Task opening = maintenance ? _service.MaintainAsync(safety.Token)
            : _service.OpenAsync("first", requester, "unlocked-opening", safety.Token);
        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.That(_store.Tickets.Single(t => t.ChannelId == ChannelOne).State, Is.EqualTo(TicketState.Creating));
            Assert.That((await _service.CloseAsync(ChannelOne + 1, Support).WaitAsync(TimeSpan.FromSeconds(2))).Success, Is.True);
            Assert.That((await _service.RenameAsync(ChannelOne + 1, Support, "unrelated-renamed").WaitAsync(TimeSpan.FromSeconds(2))).Success, Is.True);
            Assert.That((await _service.ReopenAsync(ChannelOne + 1, Support).WaitAsync(TimeSpan.FromSeconds(2))).Success, Is.True);
            Assert.That((await _service.SetHoldAsync(ChannelOne + 1, Support, true).WaitAsync(TimeSpan.FromSeconds(2))).Success, Is.True);
            Assert.That((await _service.OpenAsync("second", new Actor(3, []), "during-opening").WaitAsync(TimeSpan.FromSeconds(2))).Success, Is.True);
            var openCalls = _discord.OpenCalls;
            var duplicate = await _service.OpenAsync("first", requester, "unlocked-opening").WaitAsync(TimeSpan.FromSeconds(2));
            var samePanel = await _service.OpenAsync("first", requester, "another-opening-click").WaitAsync(TimeSpan.FromSeconds(2));
            Assert.That(duplicate.Success, Is.False);
            Assert.That(samePanel.Success, Is.False);
            Assert.That(duplicate.Message, Does.Contain("already being opened"));
            await _service.MaintainAsync().WaitAsync(TimeSpan.FromSeconds(2));
            Assert.That(_discord.OpenCalls, Is.EqualTo(openCalls), "Only the owning operation may scan/post the opening.");
            Assert.That((await _service.CloseAsync(ChannelOne, Support).WaitAsync(TimeSpan.FromSeconds(2))).Success, Is.False);
            Assert.That((await _service.SetHoldAsync(ChannelOne, Support, true).WaitAsync(TimeSpan.FromSeconds(2))).Success, Is.True);
            if (failCompletion) _store.FailNextSaveAction = "opened";
            release.SetResult();
            await opening.WaitAsync(TimeSpan.FromSeconds(2));
            var completed = _store.Tickets.Single(t => t.ChannelId == ChannelOne);
            Assert.That(completed.Hold, Is.True, "Completion/failure must merge the current durable record.");
            Assert.That(completed.State, Is.EqualTo(failCompletion ? TicketState.Creating : TicketState.Open));
            Assert.That(_discord.CreateCalls, Is.EqualTo(3));
            if (failCompletion)
            {
                _discord.BeforeProgressOpenAsync = null;
                Assert.That((await _service.OpenAsync("first", requester, "unlocked-opening")).Success, Is.True);
                Assert.That(_store.Tickets.Single(t => t.ChannelId == ChannelOne).Hold, Is.True);
                Assert.That(_discord.CreateCalls, Is.EqualTo(3), "Retry reuses the committed channel binding.");
            }
        }
        finally
        {
            release.TrySetResult();
            safety.Cancel();
            try { await opening; } catch (OperationCanceledException) { }
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task CancelledOpeningReleasesItsGuardAndRetainsBindingForRecovery(bool maintenance)
    {
        var requester = new Actor(1, []);
        _discord.FailNextOpen = true;
        Assert.That((await Open("first", requester, "cancelled-opening")).Success, Is.False);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _discord.BeforeProgressOpenAsync = async (_, progress, ct) =>
        {
            progress();
            started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
        };
        using var cancellation = new CancellationTokenSource();
        Task opening = maintenance ? _service.MaintainAsync(cancellation.Token)
            : _service.OpenAsync("first", requester, "cancelled-opening", cancellation.Token);
        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.That((await _service.SetHoldAsync(ChannelOne, Support, true).WaitAsync(TimeSpan.FromSeconds(2))).Success, Is.True);
            cancellation.Cancel();
            Assert.That(async () => await opening.WaitAsync(TimeSpan.FromSeconds(2)), Throws.InstanceOf<OperationCanceledException>());
            var durable = _store.Tickets.Single();
            Assert.That(durable.State, Is.EqualTo(TicketState.Creating));
            Assert.That(durable.ChannelId, Is.EqualTo(ChannelOne));
            Assert.That(durable.Hold, Is.True);
            _discord.BeforeProgressOpenAsync = null;
            Assert.That((await _service.OpenAsync("first", requester, "cancelled-opening")).Success, Is.True);
            Assert.That(_discord.CreateCalls, Is.EqualTo(1));
            _store.Replace(_store.Tickets.Single() with { State = TicketState.Creating });
            var restarted = new TicketService(_configuration, _store, _discord, _archive, _clock);
            await restarted.MaintainAsync();
            Assert.That(_store.Tickets.Single().State, Is.EqualTo(TicketState.Open));
            Assert.That(_store.Tickets.Single().Hold, Is.True);
            Assert.That(_discord.CreateCalls, Is.EqualTo(1));
        }
        finally { cancellation.Cancel(); try { await opening; } catch (OperationCanceledException) { } }
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task CreatingRecoveryReportsOpeningProgressAndRetriesAfterStalledLookup(bool progressing)
    {
        _discord.FailNextOpen = true;
        Assert.That((await Open("first", new Actor(1, []), "opening-progress")).Success, Is.False);
        await Open("second", new Actor(2, []), "later-missing");
        _discord.MissingChannels.Add(ChannelOne + 1);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reports = 0;
        _discord.BeforeProgressOpenAsync = async (ticket, progress, ct) =>
        {
            if (ticket.ChannelId != ChannelOne) return;
            if (!progressing)
            {
                started.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                return;
            }
            for (var page = 0; page < 6; page++)
            {
                _clock.Advance(TimeSpan.FromMinutes(1));
                ct.ThrowIfCancellationRequested();
                progress();
                reports++;
                await Task.Yield();
            }
        };
        using var safety = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var maintenance = _service.MaintainAsync(safety.Token);
        try
        {
            if (!progressing)
            {
                await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
                _clock.Advance(TimeSpan.FromMinutes(4));
            }
            await maintenance.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.That(_store.Tickets.Single(ticket => ticket.ChannelId == ChannelOne).State,
                Is.EqualTo(progressing ? TicketState.Open : TicketState.Creating));
            Assert.That(_store.Tickets.Single(ticket => ticket.ChannelId == ChannelOne + 1).State, Is.EqualTo(TicketState.Deleted));
            if (progressing)
            {
                Assert.That(reports, Is.EqualTo(6));
                Assert.That((await _service.CloseAsync(ChannelOne, new Actor(1, []))).Success, Is.True);
            }
            else
            {
                _discord.BeforeProgressOpenAsync = null;
                await _service.MaintainAsync().WaitAsync(TimeSpan.FromSeconds(2));
                Assert.That(_store.Tickets.Single(ticket => ticket.ChannelId == ChannelOne).State, Is.EqualTo(TicketState.Open));
                Assert.That(_discord.CreateCalls, Is.EqualTo(2), "Recovery must reuse the two existing managed channels.");
            }
        }
        finally { safety.Cancel(); try { await maintenance; } catch (OperationCanceledException) { } }
    }

    [TestCase(false, false, true)]
    [TestCase(false, true, true)]
    [TestCase(true, false, true)]
    [TestCase(true, true, true)]
    [TestCase(false, false, false)]
    [TestCase(false, true, false)]
    [TestCase(true, false, false)]
    [TestCase(true, true, false)]
    public async Task TranscriptDeliveryRefreshesSupportAccessAfterExport(bool blockArchive, bool departed, bool intakeEnabled)
    {
        await Open("first", new Actor(1, []), "delivery-revoked");
        _configuration.Tickets.Enabled = intakeEnabled;
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task Block(Ticket ticket, CancellationToken ct)
        {
            started.TrySetResult();
            await release.Task.WaitAsync(ct);
        }
        if (blockArchive) _archive.BeforeExportAsync = Block;
        else _discord.BeforeTranscriptAsync = Block;
        var delivered = 0;
        var export = _service.ExportAsync(ChannelOne, Support, deliver: (_, _) =>
        {
            delivered++;
            return Task.CompletedTask;
        });
        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
            if (departed) _discord.MissingMembers.Add(Support.UserId);
            else _discord.CurrentActors[Support.UserId] = new Actor(Support.UserId, []);
            release.SetResult();
            if (departed) Assert.ThrowsAsync<InvalidOperationException>(async () => await export);
            else Assert.That((await export).Success, Is.False);
            Assert.That(delivered, Is.Zero, "A stale support actor must not authorize private attachment delivery.");
            Assert.That(_discord.ActorLookups.TakeLast(1), Is.EqualTo(new[] { Support.UserId }));
            Assert.That(_store.Tickets.Single().ArchiveComplete, Is.True, "The owned archive remains usable by authorized staff.");
            _discord.MissingMembers.Remove(Support.UserId);
            _discord.CurrentActors[Support.UserId] = Support;
            Assert.That((await _service.ExportAsync(ChannelOne, Support, deliver: (_, _) =>
            {
                delivered++;
                return Task.CompletedTask;
            })).Success, Is.True, "Authorization failure must release the export guard.");
            Assert.That(delivered, Is.EqualTo(1));
        }
        finally
        {
            release.TrySetResult();
            try { await export; } catch (InvalidOperationException) when (departed) { }
        }
    }
    [TestCase(TicketState.Open, true)]
    [TestCase(TicketState.Open, false)]
    [TestCase(TicketState.Closed, true)]
    [TestCase(TicketState.Closed, false)]
    public async Task RestartReconcilesRetainedPermissionsWithoutChangingTicketStateOrMetadata(TicketState state, bool enabled)
    {
        var ticket = (await Open("first", new Actor(1, []), "restart-policy")).Ticket!;
        if (state == TicketState.Closed) await _service.CloseAsync(ChannelOne, Support);
        ticket = _store.Tickets.Single() with { Hold = true, ArchivePath = "/archives/retained", ArchiveComplete = true };
        _store.Replace(ticket);
        _configuration.Tickets.SupportRoleIds = [901];
        _configuration.Tickets.ClosedRequesterCanRead = false;
        _configuration.Tickets.Enabled = enabled;
        _service = new TicketService(_configuration, _store, _discord, _archive, _clock);
        var openCalls = _discord.OpenCalls;
        var closeCalls = _discord.CloseCalls;

        await _service.ReconcileRetainedPermissionsAsync();
        await _service.MaintainAsync();

        Assert.That(_discord.ReconciledTickets.Select(value => value.State), Is.EqualTo(new[] { state, state }),
            "Both startup and maintenance must cover held, not-yet-due stable tickets even when new ticketing is disabled.");
        Assert.That(_store.Tickets.Single(), Is.EqualTo(ticket));
        Assert.That(_discord.OpenCalls, Is.EqualTo(openCalls));
        Assert.That(_discord.CloseCalls, Is.EqualTo(closeCalls));
        Assert.That(_discord.FreezeCalls, Is.Zero);
        Assert.That(_discord.DeleteCalls, Is.Zero);
        Assert.That(_archive.ExportCalls, Is.Zero);
    }

    [Test]
    public async Task PermissionReconciliationFailureStopsStartupAndRetainsMaintenanceForRetry()
    {
        await Open("first", new Actor(1, []), "bad-policy");
        await _service.CloseAsync(ChannelOne, Support);
        _clock.Advance(TimeSpan.FromDays(8));
        _discord.BeforeReconcileAsync = (_, _) => Task.FromException(new InvalidOperationException("Permission update failed."));

        Assert.ThrowsAsync<InvalidOperationException>(() => _service.ReconcileRetainedPermissionsAsync());
        await _service.MaintainAsync();

        Assert.That(_store.Tickets.Single().State, Is.EqualTo(TicketState.Closed));
        Assert.That(_store.Tickets.Single().LastError, Does.Contain("Maintenance failed"));
        Assert.That(_discord.DeleteCalls, Is.Zero);
        Assert.That(_archive.ExportCalls, Is.Zero);
        _discord.BeforeReconcileAsync = null;
        await _service.ReconcileRetainedPermissionsAsync();
        await _service.MaintainAsync();
        Assert.That(_store.Tickets.Single().State, Is.EqualTo(TicketState.Deleted));
    }

    [Test]
    public async Task MissingRetainedChannelDoesNotBlockPermissionStartupAndIsReconciledByMaintenance()
    {
        await Open("first", new Actor(1, []), "missing-policy");
        _discord.MissingChannels.Add(ChannelOne);

        await _service.ReconcileRetainedPermissionsAsync();
        Assert.That(_discord.ReconciledTickets, Is.Empty);
        await _service.MaintainAsync();

        Assert.That(_store.Tickets.Single().State, Is.EqualTo(TicketState.Deleted));
        Assert.That(_discord.ReconciledTickets, Is.Empty);
    }

    [Test]
    public async Task StartupAndConcurrentMaintenanceNeverRestorePermissionsDuringDeletingExport()
    {
        await Open("first", new Actor(1, []), "frozen-policy");
        await _service.CloseAsync(ChannelOne, Support);
        _clock.Advance(TimeSpan.FromDays(8));
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _discord.BeforeTranscriptAsync = async (_, ct) =>
        {
            started.TrySetResult();
            await release.Task.WaitAsync(ct);
        };
        var cleanup = _service.MaintainAsync();
        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
            var reconciles = _discord.ReconciledTickets.Count;
            var freezes = _discord.FreezeCalls;
            Assert.That(_store.Tickets.Single().State, Is.EqualTo(TicketState.Deleting));
            await _service.ReconcileRetainedPermissionsAsync().WaitAsync(TimeSpan.FromSeconds(2));
            await _service.MaintainAsync().WaitAsync(TimeSpan.FromSeconds(2));
            Assert.That(_discord.ReconciledTickets.Count, Is.EqualTo(reconciles));
            Assert.That(_discord.FreezeCalls, Is.EqualTo(freezes));
            Assert.That(cleanup.IsCompleted, Is.False);
            release.SetResult();
            await cleanup.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.That(_store.Tickets.Single().State, Is.EqualTo(TicketState.Deleted));
        }
        finally { release.TrySetResult(); await cleanup; }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task StableTicketPolicyCanReconcileDuringManualExportWithoutReleasingItsGuard(bool closed)
    {
        await Open("first", new Actor(1, []), "manual-policy");
        if (closed) await _service.CloseAsync(ChannelOne, Support);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _discord.BeforeTranscriptAsync = async (_, ct) =>
        {
            started.TrySetResult();
            await release.Task.WaitAsync(ct);
        };
        var export = _service.ExportAsync(ChannelOne, Support);
        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
            await _service.ReconcileRetainedPermissionsAsync().WaitAsync(TimeSpan.FromSeconds(2));
            await _service.MaintainAsync().WaitAsync(TimeSpan.FromSeconds(2));
            Assert.That(_discord.ReconciledTickets, Has.Count.EqualTo(2));
            Assert.That(_discord.ReconciledTickets.All(ticket => ticket.State == (closed ? TicketState.Closed : TicketState.Open)), Is.True);
            Assert.That((await _service.ExportAsync(ChannelOne, Support)).Success, Is.False);
            Assert.That(_discord.FreezeCalls, Is.Zero);
            Assert.That(_discord.DeleteCalls, Is.Zero);
            release.SetResult();
            Assert.That((await export).Success, Is.True);
        }
        finally { release.TrySetResult(); await export; }
    }
    private async Task<TicketResult> Open(string panel, Actor actor, string interactionId) =>
        await _service.OpenAsync(panel, actor, interactionId);

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private readonly object _gate = new();
        private readonly List<ManualTimer> _timers = [];
        private DateTimeOffset _now = now;
        public bool HasExpirationBackoff { get { lock (_gate) return _timers.Any(t => t.IsExpirationBackoff); } }
        public void AdvanceExpirationBackoff()
        {
            DateTimeOffset? due;
            lock (_gate) due = _timers.Where(t => t.IsExpirationBackoff).Select(t => t.DueAt).FirstOrDefault();
            if (due is not null) Advance(due.Value - GetUtcNow());
        }
        public override DateTimeOffset GetUtcNow() { lock (_gate) return _now; }
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ManualTimer(this, callback, state);
            lock (_gate) _timers.Add(timer);
            timer.Change(dueTime, period);
            return timer;
        }
        public void Advance(TimeSpan duration)
        {
            ManualTimer[] timers;
            lock (_gate) { _now += duration; timers = _timers.ToArray(); }
            foreach (var timer in timers) timer.FireIfDue();
        }

        private sealed class ManualTimer(MutableTimeProvider owner, TimerCallback callback, object? state) : ITimer
        {
            private DateTimeOffset? _dueAt;
            private TimeSpan _period;
            public DateTimeOffset? DueAt => _dueAt;
            public bool IsExpirationBackoff => !_disposed && _dueAt is { } due && due >= owner._now &&
                due - owner._now <= TimeSpan.FromSeconds(10);
            private bool _disposed;
            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                lock (owner._gate)
                {
                    if (_disposed) return false;
                    _dueAt = dueTime == Timeout.InfiniteTimeSpan ? null : owner._now + dueTime;
                    _period = period;
                    return true;
                }
            }
            public void FireIfDue()
            {
                lock (owner._gate)
                {
                    if (_disposed || _dueAt is null || _dueAt > owner._now) return;
                    _dueAt = _period > TimeSpan.Zero ? owner._now + _period : null;
                }
                callback(state);
            }
            public void Dispose()
            {
                lock (owner._gate) { _disposed = true; owner._timers.Remove(this); }
            }
            public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
        }
    }

    private sealed class MemoryTicketStore : ITicketStore
    {
        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly List<Ticket> _tickets = [];
        private readonly Dictionary<string, Guid> _interactions = new(StringComparer.Ordinal);
        private readonly HashSet<string> _deliveries = new(StringComparer.Ordinal);
        public IReadOnlyList<Ticket> Tickets => _tickets.ToArray();
        public string? FailNextSaveAction { get; set; }
        public bool FailNextSaveWithCancellation { get; set; }
        public Action? BeforeSaveCancellation { get; set; }
        public int TicketEnumerationCount { get; set; }
        public List<string> Audits { get; } = [];

        public Task InitializeAsync(CancellationToken ct) => Task.CompletedTask;

        public async Task<ITicketSession> LockAsync(CancellationToken ct)
        {
            await _gate.WaitAsync(ct);
            return new Session(this);
        }

        public void Replace(Ticket ticket)
        {
            var index = _tickets.FindIndex(x => x.Id == ticket.Id);
            if (index < 0) throw new InvalidOperationException("Cannot replace an unknown ticket.");
            _tickets[index] = ticket;
        }

        private sealed class Session(MemoryTicketStore store) : ITicketSession
        {
            private bool _disposed;

            public Task<IReadOnlyList<Ticket>> GetTicketsAsync(CancellationToken ct) =>
                EnumerateTickets();

            private Task<IReadOnlyList<Ticket>> EnumerateTickets()
            {
                store.TicketEnumerationCount++;
                return Task.FromResult<IReadOnlyList<Ticket>>(store._tickets.ToArray());
            }

            public Task<Ticket?> GetTicketAsync(Guid id, CancellationToken ct) =>
                Task.FromResult(store._tickets.SingleOrDefault(ticket => ticket.Id == id));

            public Task<Ticket?> FindByChannelAsync(ulong channelId, CancellationToken ct) =>
                Task.FromResult(store._tickets.SingleOrDefault(ticket => ticket.ChannelId == channelId));

            public Task<Ticket?> FindInteractionAsync(string interactionId, CancellationToken ct)
            {
                if (!store._interactions.TryGetValue(interactionId, out var id)) return Task.FromResult<Ticket?>(null);
                return Task.FromResult(store._tickets.SingleOrDefault(x => x.Id == id));
            }

            public Task<Ticket> ReserveAsync(string panelId, ulong requesterId, string interactionId,
                DateTimeOffset now, CancellationToken ct)
            {
                var ticket = new Ticket(Guid.NewGuid(), panelId, requesterId, null, TicketState.Creating,
                    store._tickets.Count == 0 ? 1 : store._tickets.Max(x => x.Number) + 1, now);
                store._tickets.Add(ticket);
                store._interactions.Add(interactionId, ticket.Id);
                return Task.FromResult(ticket);
            }

            public Task SaveAsync(Ticket ticket, string action, ulong? actorId, CancellationToken ct)
            {
                if (store.FailNextSaveAction == action)
                {
                    store.FailNextSaveAction = null;
                    if (store.FailNextSaveWithCancellation)
                    {
                        store.FailNextSaveWithCancellation = false;
                        store.BeforeSaveCancellation?.Invoke();
                        throw new OperationCanceledException("Simulated interruption before the channel binding committed.");
                    }
                    throw new InvalidOperationException("Simulated persistence failure.");
                }
                store.Replace(ticket);
                store.Audits.Add(action);
                return Task.CompletedTask;
            }

            public Task<bool> TryRecordDeliveryAsync(string key, CancellationToken ct) =>
                Task.FromResult(store._deliveries.Add(key));

            public Task<DeliveryState> GetOrCreateDeliveryAsync(string key, string intent, CancellationToken ct) =>
                throw new NotSupportedException("TicketServiceTests do not exercise community-delivery persistence.");

            public Task CompleteDeliveryAsync(string key, CancellationToken ct) =>
                throw new NotSupportedException("TicketServiceTests do not exercise community-delivery persistence.");

            public Task<DateTimeOffset?> GetCooldownAsync(string key, CancellationToken ct) =>
                throw new NotSupportedException("TicketServiceTests do not exercise command cooldown persistence.");

            public Task SetCooldownAsync(string key, DateTimeOffset at, CancellationToken ct) =>
                throw new NotSupportedException("TicketServiceTests do not exercise command cooldown persistence.");
            public Task<bool> TryAdvanceCommunityActionAsync(string scope, ulong messageId, CancellationToken ct) =>
                throw new NotSupportedException("TicketServiceTests do not exercise community action ordering.");
            public ValueTask DisposeAsync()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    store._gate.Release();
                }
                return ValueTask.CompletedTask;
            }
        }
    }

    private sealed class FakeDiscordTickets : IDiscordTickets
    {
        private readonly Dictionary<Guid, ulong> _managedChannels = [];
        private ulong _nextChannel = ChannelOne;
        public int CreateCalls { get; private set; }
        public int FindManagedCalls { get; private set; }
        public Func<Guid, CancellationToken, Task>? BeforeFindManagedAsync { get; set; }
        public Func<Ticket, CancellationToken, Task>? BeforeCreateAsync { get; set; }
        public Func<Ticket, CancellationToken, Task>? AfterCreateAsync { get; set; }
        public int OpenCalls { get; private set; }
        public int CloseCalls { get; private set; }
        public int RenameCalls { get; private set; }
        public int ExistsCalls { get; private set; }
        public HashSet<ulong> MissingChannels { get; } = [];
        public bool FailNextExists { get; set; }
        public Func<Ticket, Task>? BeforeExistsAsync { get; set; }
        public int FreezeCalls { get; private set; }
        public Func<Ticket, CancellationToken, Task>? BeforeFreezeAsync { get; set; }
        public Func<Ticket, CancellationToken, Task>? BeforeLastMessageIdAsync { get; set; }
        public Func<Ticket, CancellationToken, Task>? BeforeDeleteAsync { get; set; }
        public List<Ticket> ReconciledTickets { get; } = [];
        public Func<Ticket, CancellationToken, Task>? BeforeReconcileAsync { get; set; }
        public int DeleteCalls { get; private set; }
        public List<ulong> DeletedChannels { get; } = [];
        public bool FailNextOpen { get; set; }
        public Func<Ticket, Action, CancellationToken, Task>? BeforeProgressOpenAsync { get; set; }
        public bool FailNextClose { get; set; }
        public Func<Ticket, CancellationToken, Task>? BeforeTranscriptAsync { get; set; }
        public Func<Ticket, Action, CancellationToken, Task>? BeforeProgressTranscriptAsync { get; set; }
        public TranscriptSnapshot Snapshot { get; set; } = new([], 101);
        public ulong? LastMessageIdAfterRead { get; set; }

        public Dictionary<ulong, Actor> CurrentActors { get; } = [];
        public HashSet<ulong> MissingMembers { get; } = [];
        public List<ulong> ActorLookups { get; } = [];
        public Task<Actor> ActorAsync(ulong userId, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            ActorLookups.Add(userId);
            if (MissingMembers.Contains(userId)) throw new InvalidOperationException("Requester is no longer a guild member.");
            return Task.FromResult(CurrentActors.GetValueOrDefault(userId) ?? new Actor(userId, []));
        }
        public async Task<ulong?> FindManagedChannelAsync(Guid ticketId, CancellationToken ct)
        {
            FindManagedCalls++;
            ct.ThrowIfCancellationRequested();
            if (BeforeFindManagedAsync is not null) await BeforeFindManagedAsync(ticketId, ct);
            ct.ThrowIfCancellationRequested();
            return _managedChannels.TryGetValue(ticketId, out var channel) ? channel : null;
        }

        public async Task<ulong> CreateAsync(Ticket ticket, CancellationToken ct)
        {
            CreateCalls++;
            ct.ThrowIfCancellationRequested();
            if (BeforeCreateAsync is not null) await BeforeCreateAsync(ticket, ct);
            ct.ThrowIfCancellationRequested();
            var channel = _nextChannel++;
            _managedChannels[ticket.Id] = channel;
            if (AfterCreateAsync is not null) await AfterCreateAsync(ticket, ct);
            ct.ThrowIfCancellationRequested();
            return channel;
        }

        public Task OpenAsync(Ticket ticket, bool sendOpeningMessage, CancellationToken ct)
        {
            OpenCalls++;
            if (FailNextOpen)
            {
                FailNextOpen = false;
                throw new InvalidOperationException("Simulated Discord open failure.");
            }
            return Task.CompletedTask;
        }

        public async Task OpenAsync(Ticket ticket, bool sendOpeningMessage, CancellationToken ct, Action progress)
        {
            await OpenAsync(ticket, sendOpeningMessage, ct);
            if (BeforeProgressOpenAsync is not null) await BeforeProgressOpenAsync(ticket, progress, ct);
            ct.ThrowIfCancellationRequested();
            progress();
        }

        public Task CloseAsync(Ticket ticket, CancellationToken ct)
        {
            CloseCalls++;
            if (FailNextClose)
            {
                FailNextClose = false;
                throw new InvalidOperationException("Simulated Discord close failure.");
            }
            return Task.CompletedTask;
        }

        public Task RenameAsync(Ticket ticket, string name, CancellationToken ct)
        {
            RenameCalls++;
            return Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(Ticket ticket, CancellationToken ct)
        {
            ExistsCalls++;
            if (BeforeExistsAsync is not null) await BeforeExistsAsync(ticket);
            if (FailNextExists)
            {
                FailNextExists = false;
                throw new InvalidOperationException("Simulated channel lookup failure; not a confirmed missing channel.");
            }
            return ticket.ChannelId.HasValue && !MissingChannels.Contains(ticket.ChannelId.Value);
        }

        public async Task ReconcilePermissionsAsync(Ticket ticket, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (BeforeReconcileAsync is not null) await BeforeReconcileAsync(ticket, ct);
            ReconciledTickets.Add(ticket);
        }
        public async Task FreezeAsync(Ticket ticket, CancellationToken ct)
        {
            FreezeCalls++;
            if (BeforeFreezeAsync is not null) await BeforeFreezeAsync(ticket, ct);
        }

        public async Task<TranscriptSnapshot> ReadTranscriptAsync(Ticket ticket, CancellationToken ct)
        {
            if (BeforeTranscriptAsync is not null) await BeforeTranscriptAsync(ticket, ct);
            return Snapshot;
        }

        public async Task<TranscriptSnapshot> ReadTranscriptAsync(Ticket ticket, CancellationToken ct, Action progress)
        {
            if (BeforeProgressTranscriptAsync is not null) await BeforeProgressTranscriptAsync(ticket, progress, ct);
            return await ReadTranscriptAsync(ticket, ct);
        }
        public async Task<ulong?> LastMessageIdAsync(Ticket ticket, CancellationToken ct)
        {
            if (BeforeLastMessageIdAsync is not null) await BeforeLastMessageIdAsync(ticket, ct);
            return LastMessageIdAfterRead ?? Snapshot.LastMessageId;
        }

        public async Task DeleteAsync(Ticket ticket, CancellationToken ct)
        {
            if (BeforeDeleteAsync is not null) await BeforeDeleteAsync(ticket, ct);
            DeleteCalls++;
            if (ticket.ChannelId is ulong channel) DeletedChannels.Add(channel);
        }

        public Task LogAsync(string message, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeArchive : ITranscriptArchive
    {
        public bool FailExports { get; set; }
        public int ExportCalls { get; private set; }
        public Func<Ticket, CancellationToken, Task>? BeforeExportAsync { get; set; }
        public Func<Ticket, Action, CancellationToken, Task>? BeforeProgressExportAsync { get; set; }
        public Func<Ticket, Action, CancellationToken, Task>? BeforePruneAsync { get; set; }
        public int PruneCalls { get; private set; }
        public List<string> DeletedPaths { get; } = [];
        public Func<string, Action, CancellationToken, Task>? BeforeDeleteArchiveAsync { get; set; }

        public string GetArchivePath(Ticket ticket) => $"/archives/{ticket.Id:N}.json";

        public async Task<string> ExportAsync(Ticket ticket, TranscriptSnapshot snapshot, CancellationToken ct)
        {
            ExportCalls++;
            if (BeforeExportAsync is not null) await BeforeExportAsync(ticket, ct);
            if (FailExports) throw new InvalidOperationException("Simulated archive storage failure.");
            return $"/archives/{ticket.Id:N}.json/snapshots/{ExportCalls}";
        }

        public async Task<string> ExportAsync(Ticket ticket, TranscriptSnapshot snapshot, CancellationToken ct, Action progress)
        {
            if (BeforeProgressExportAsync is not null) await BeforeProgressExportAsync(ticket, progress, ct);
            return await ExportAsync(ticket, snapshot, ct);
        }
        public Task PruneSnapshotsAsync(Ticket ticket, CancellationToken ct) =>
            PruneSnapshotsAsync(ticket, ct, static () => { });

        public async Task PruneSnapshotsAsync(Ticket ticket, CancellationToken ct, Action progress)
        {
            ct.ThrowIfCancellationRequested();
            PruneCalls++;
            if (BeforePruneAsync is not null) await BeforePruneAsync(ticket, progress, ct);
            ct.ThrowIfCancellationRequested();
        }
        public Task DeleteAsync(string path, CancellationToken ct) => DeleteAsync(path, ct, static () => { });

        public async Task DeleteAsync(string path, CancellationToken ct, Action progress)
        {
            ct.ThrowIfCancellationRequested();
            if (BeforeDeleteArchiveAsync is not null) await BeforeDeleteArchiveAsync(path, progress, ct);
            ct.ThrowIfCancellationRequested();
            DeletedPaths.Add(path);
            progress();
        }
    }
}
