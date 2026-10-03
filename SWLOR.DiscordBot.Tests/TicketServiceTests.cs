using NUnit.Framework;
using SWLOR.DiscordBot.Configuration;
using SWLOR.DiscordBot.Core;

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

        Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await _service.OpenAsync("first", requester, "interrupted-create");
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

        public Task<TranscriptSnapshot> ReadTranscriptAsync(Ticket ticket, CancellationToken ct) => Task.FromResult(Snapshot);

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
        public List<string> DeletedPaths { get; } = [];

        public Task<string> ExportAsync(Ticket ticket, TranscriptSnapshot snapshot, CancellationToken ct)
        {
            ExportCalls++;
            if (FailExports) throw new InvalidOperationException("Simulated archive storage failure.");
            return Task.FromResult($"/archives/{ticket.Id:N}.json");
        }

        public Task DeleteAsync(string path, CancellationToken ct)
        {
            DeletedPaths.Add(path);
            return Task.CompletedTask;
        }
    }
}
