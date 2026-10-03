using NUnit.Framework;
using SWLOR.DiscordBot.Configuration;
using SWLOR.DiscordBot.Core;
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
        _archive = new FakeArchive();
        _clock = new MutableTimeProvider(new DateTimeOffset(2026, 10, 3, 12, 0, 0, TimeSpan.Zero));
        _service = new TicketService(_configuration, _store, _discord, _archive, _clock);
    }

    [Test]
    public async Task NonSupportCannotCloseExportRenameOrReopenAnotherMembersTicket()
    {
        var requester = new Actor(1, []);
        var opened = await Open("first", requester, "unauthorized-check");
        var stranger = new Actor(2, []);

        var close = await _service.CloseAsync(ChannelOne, stranger);
        var export = await _service.ExportAsync(ChannelOne, stranger);
        var rename = await _service.RenameAsync(ChannelOne, stranger, "hacked name");
        var closeBySupport = await _service.CloseAsync(ChannelOne, Support);
        var reopen = await _service.ReopenAsync(ChannelOne, stranger);

        Assert.Multiple(() =>
        {
            Assert.That(opened.Success, Is.True);
            Assert.That(close.Success, Is.False);
            Assert.That(export.Success, Is.False);
            Assert.That(rename.Success, Is.False);
            Assert.That(reopen.Success, Is.False);
            Assert.That(closeBySupport.Success, Is.True);
            Assert.That(_discord.CloseCalls, Is.EqualTo(1));
            Assert.That(_discord.RenameCalls, Is.Zero);
            Assert.That(_archive.ExportCalls, Is.Zero);
            Assert.That(_discord.OpenCalls, Is.EqualTo(1), "unauthorized reopen must not open the channel");
        });
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

    [TestCase("edit")]
    [TestCase("delete")]
    [TestCase("embed")]
    [TestCase("attachment")]
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
            Assert.That(File.Exists(Path.Combine(pending.ArchivePath!, "transcript.html")), Is.True);
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

    private async Task<TicketResult> Open(string panel, Actor actor, string interactionId) =>
        await _service.OpenAsync(panel, actor, interactionId);

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan duration) => _now += duration;
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
                Task.FromResult<IReadOnlyList<Ticket>>(store._tickets.ToArray());

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
        public int OpenCalls { get; private set; }
        public int CloseCalls { get; private set; }
        public int RenameCalls { get; private set; }
        public int ExistsCalls { get; private set; }
        public HashSet<ulong> MissingChannels { get; } = [];
        public bool FailNextExists { get; set; }
        public Func<Ticket, Task>? BeforeExistsAsync { get; set; }
        public int FreezeCalls { get; private set; }
        public int DeleteCalls { get; private set; }
        public List<ulong> DeletedChannels { get; } = [];
        public bool FailNextOpen { get; set; }
        public bool FailNextClose { get; set; }
        public Func<Ticket, CancellationToken, Task>? BeforeTranscriptAsync { get; set; }
        public TranscriptSnapshot Snapshot { get; set; } = new([], 101);
        public ulong? LastMessageIdAfterRead { get; set; }

        public Task<ulong?> FindManagedChannelAsync(Guid ticketId, CancellationToken ct)
        {
            FindManagedCalls++;
            return Task.FromResult(_managedChannels.TryGetValue(ticketId, out var channel) ? (ulong?)channel : null);
        }

        public Task<ulong> CreateAsync(Ticket ticket, CancellationToken ct)
        {
            CreateCalls++;
            var channel = _nextChannel++;
            _managedChannels[ticket.Id] = channel;
            return Task.FromResult(channel);
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

        public Task FreezeAsync(Ticket ticket, CancellationToken ct)
        {
            FreezeCalls++;
            return Task.CompletedTask;
        }

        public async Task<TranscriptSnapshot> ReadTranscriptAsync(Ticket ticket, CancellationToken ct)
        {
            if (BeforeTranscriptAsync is not null) await BeforeTranscriptAsync(ticket, ct);
            return Snapshot;
        }

        public Task<ulong?> LastMessageIdAsync(Ticket ticket, CancellationToken ct) =>
            Task.FromResult(LastMessageIdAfterRead ?? Snapshot.LastMessageId);

        public Task DeleteAsync(Ticket ticket, CancellationToken ct)
        {
            DeleteCalls++;
            if (ticket.ChannelId is ulong channel) DeletedChannels.Add(channel);
            return Task.CompletedTask;
        }

        public Task LogAsync(string message, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeArchive : ITranscriptArchive
    {
        public bool FailExports { get; set; }
        public int ExportCalls { get; private set; }
        public Func<Ticket, CancellationToken, Task>? BeforeExportAsync { get; set; }
        public List<string> DeletedPaths { get; } = [];

        public string GetArchivePath(Ticket ticket) => $"/archives/{ticket.Id:N}.json";

        public async Task<string> ExportAsync(Ticket ticket, TranscriptSnapshot snapshot, CancellationToken ct)
        {
            ExportCalls++;
            if (BeforeExportAsync is not null) await BeforeExportAsync(ticket, ct);
            if (FailExports) throw new InvalidOperationException("Simulated archive storage failure.");
            return $"/archives/{ticket.Id:N}.json";
        }

        public Task DeleteAsync(string path, CancellationToken ct)
        {
            DeletedPaths.Add(path);
            return Task.CompletedTask;
        }
    }
}
