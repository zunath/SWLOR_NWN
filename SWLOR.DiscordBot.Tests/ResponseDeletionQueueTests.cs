using System.Net;
using Discord.Net;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using SWLOR.DiscordBot.Core;
using SWLOR.DiscordBot.Hosting;

namespace SWLOR.DiscordBot.Tests;

[TestFixture]
public sealed class ResponseDeletionQueueTests
{
    [Test]
    public async Task RecreatedQueueDeletesOnlyDueMessagesInTheirOriginalChannels()
    {
        var clock = new Clock();
        var store = new MemoryDeletionStore();
        await Queue(store, clock).ScheduleAsync(ulong.MaxValue, 11, TimeSpan.FromMinutes(1), default);
        await Queue(store, clock).ScheduleAsync(12, 13, TimeSpan.FromMinutes(2), default);
        var discord = new Discord();
        var restarted = Queue(store, clock);
        await restarted.DeleteDueAsync(discord, default);
        Assert.That(discord.Deleted, Is.Empty);
        clock.Now += TimeSpan.FromMinutes(1);
        await restarted.DeleteDueAsync(discord, default);
        Assert.That(discord.Deleted, Is.EqualTo(new[] { (ulong.MaxValue, 11UL) }));
        clock.Now += TimeSpan.FromMinutes(1);
        await restarted.DeleteDueAsync(discord, default);
        Assert.That(discord.Deleted, Is.EqualTo(new[] { (ulong.MaxValue, 11UL), (12UL, 13UL) }));
    }

    [Test]
    public async Task ShutdownCancellationDuringDiscordRequestRetainsPendingIntent()
    {
        var clock = new Clock();
        var store = new MemoryDeletionStore();
        await Queue(store, clock).ScheduleAsync(10, 11, TimeSpan.Zero, default);
        using var cancellation = new CancellationTokenSource();
        var discord = new Discord
        {
            Delete = (_, _, _) => { cancellation.Cancel(); throw new OperationCanceledException(cancellation.Token); }
        };
        Assert.ThrowsAsync<OperationCanceledException>(() => Queue(store, clock).DeleteDueAsync(discord, cancellation.Token));
        Assert.That((await store.GetDueDeletionsAsync(clock.Now, default)).Single().Attempts, Is.Zero);
        await Queue(store, clock).DeleteDueAsync(new Discord(), default);
        Assert.That(await store.GetDueDeletionsAsync(clock.Now, default), Is.Empty);
    }

    [Test]
    public async Task ShutdownAfterDiscordDeletionBeforeDbCompletionRetainsRetryableIntent()
    {
        var clock = new Clock();
        var store = new MemoryDeletionStore();
        await Queue(store, clock).ScheduleAsync(10, 11, TimeSpan.Zero, default);
        using var cancellation = new CancellationTokenSource();
        var discord = new Discord { Delete = (_, _, _) => { cancellation.Cancel(); return Task.CompletedTask; } };
        Assert.ThrowsAsync<OperationCanceledException>(() => Queue(store, clock).DeleteDueAsync(discord, cancellation.Token));
        Assert.That(await store.GetDueDeletionsAsync(clock.Now, default), Has.Count.EqualTo(1));
        await Queue(store, clock).DeleteDueAsync(new Discord
        {
            Delete = (_, _, _) => throw new HttpException(HttpStatusCode.NotFound, null!, null, "Missing message.", [])
        }, default);
        Assert.That(await store.GetDueDeletionsAsync(clock.Now, default), Is.Empty);
    }

    [Test]
    public async Task ForbiddenKeepsActionableRetryAcrossRestartsAndRepeatedSchedules()
    {
        var clock = new Clock();
        var store = new MemoryDeletionStore();
        await Queue(store, clock).ScheduleAsync(10, 11, TimeSpan.Zero, default);
        var forbidden = new Discord { Delete = (_, _, _) => throw new HttpException(HttpStatusCode.Forbidden, null!, null, "Forbidden.", []) };
        for (var attempt = 0; attempt < 10; attempt++)
        {
            await Queue(store, clock).DeleteDueAsync(forbidden, default);
            var pending = store.Pending.Single().Value;
            Assert.That(pending.Attempts, Is.EqualTo(attempt + 1));
            Assert.That(pending.LastError, Does.Contain("restore channel access"));
            Assert.That(pending.DueAt - clock.Now, Is.GreaterThan(TimeSpan.Zero).And.LessThanOrEqualTo(TimeSpan.FromHours(1)));
            await Queue(store, clock).ScheduleAsync(10, 11, TimeSpan.FromDays(10), default);
            Assert.That(store.Pending.Single().Value, Is.EqualTo(pending));
            clock.Now = pending.DueAt;
        }
        await Queue(store, clock).DeleteDueAsync(new Discord(), default);
        Assert.That(await store.GetDueDeletionsAsync(clock.Now, default), Is.Empty);
    }

    [Test]
    public async Task TransientFailurePersistsRetryMetadataInsteadOfDroppingMessage()
    {
        var clock = new Clock();
        var store = new MemoryDeletionStore();
        await Queue(store, clock).ScheduleAsync(10, 11, TimeSpan.Zero, default);
        await Queue(store, clock).DeleteDueAsync(new Discord
        {
            Delete = (_, _, _) => throw new HttpRequestException("unavailable")
        }, default);
        var pending = store.Pending.Single().Value;
        Assert.That(pending.Attempts, Is.EqualTo(1));
        Assert.That(pending.LastError, Is.EqualTo(nameof(HttpRequestException)));
        Assert.That(await store.GetDueDeletionsAsync(clock.Now, default), Is.Empty);
        clock.Now = pending.DueAt;
        await Queue(store, clock).DeleteDueAsync(new Discord(), default);
        Assert.That(await store.GetDueDeletionsAsync(clock.Now, default), Is.Empty);
    }

    [Test]
    public async Task NotFoundCompletesPermanentlyAndDuplicateScheduleDoesNotResurrectDeletion()
    {
        var clock = new Clock();
        var store = new MemoryDeletionStore();
        await Queue(store, clock).ScheduleAsync(10, 11, TimeSpan.Zero, default);
        await Queue(store, clock).DeleteDueAsync(new Discord
        {
            Delete = (_, _, _) => throw new HttpException(HttpStatusCode.NotFound, null!, null, "Missing message.", [])
        }, default);
        await Queue(store, clock).ScheduleAsync(10, 11, TimeSpan.Zero, default);
        Assert.That(await store.GetDueDeletionsAsync(clock.Now, default), Is.Empty);
    }

    private static ResponseDeletionQueue Queue(MemoryDeletionStore store, Clock clock) =>
        new(store, clock, NullLogger<ResponseDeletionQueue>.Instance);

    private sealed class Clock : TimeProvider
    {
        internal DateTimeOffset Now = new(2026, 10, 3, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class MemoryDeletionStore : IResponseDeletionStore
    {
        internal readonly Dictionary<(ulong, ulong), PendingResponseDeletion> Pending = new();
        private readonly HashSet<(ulong, ulong)> completed = [];
        public Task ScheduleDeletionAsync(ulong channelId, ulong messageId, DateTimeOffset dueAt, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Pending.TryAdd((channelId, messageId), new(channelId, messageId, dueAt));
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<PendingResponseDeletion>> GetDueDeletionsAsync(DateTimeOffset now, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<PendingResponseDeletion>>(Pending
                .Where(x => !completed.Contains(x.Key) && x.Value.DueAt <= now).Select(x => x.Value).ToArray());
        }
        public Task CompleteDeletionAsync(ulong channelId, ulong messageId, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            completed.Add((channelId, messageId));
            return Task.CompletedTask;
        }
        public Task RetryDeletionAsync(PendingResponseDeletion deletion, DateTimeOffset dueAt, string error, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Pending[(deletion.ChannelId, deletion.MessageId)] = deletion with
                { DueAt = dueAt, Attempts = deletion.Attempts + 1, LastError = error };
            return Task.CompletedTask;
        }
    }

    private sealed class Discord : ICommunityDiscord
    {
        internal Func<ulong, ulong, CancellationToken, Task> Delete = (_, _, _) => Task.CompletedTask;
        internal readonly List<(ulong, ulong)> Deleted = [];
        public string ServerName => "Test";
        public async Task DeleteMessageAsync(ulong channelId, ulong messageId, CancellationToken ct)
        {
            await Delete(channelId, messageId, ct);
            Deleted.Add((channelId, messageId));
        }
        public Task<CommunityMember?> GetMemberAsync(ulong userId, CancellationToken ct) => throw new NotSupportedException();
        public Task<CommunityRole?> GetRoleAsync(ulong roleId, CancellationToken ct) => throw new NotSupportedException();
        public Task AddRoleAsync(ulong userId, ulong roleId, CancellationToken ct) => throw new NotSupportedException();
        public Task RemoveRoleAsync(ulong userId, ulong roleId, CancellationToken ct) => throw new NotSupportedException();
        public Task<ulong?> SendAsync(ulong channelId, CommunityMessage message, CancellationToken ct) => throw new NotSupportedException();
        public Task<ulong?> SendDirectMessageAsync(ulong userId, CommunityMessage message, CancellationToken ct) => throw new NotSupportedException();
    }
}
