using NUnit.Framework;
using SWLOR.DiscordBot.Core;
using SWLOR.DiscordBot.Persistence;

namespace SWLOR.DiscordBot.Tests;

[TestFixture]
public sealed class PostgresStoreTests
{
    private static string ConnectionString => Environment.GetEnvironmentVariable("SWLOR_BOT_TEST_DATABASE")
        ?? throw new InvalidOperationException("Set SWLOR_BOT_TEST_DATABASE to an isolated test database.");

    [Test]
    public async Task TicketAndPendingIntentSurviveStoreRestart()
    {
        var key = Guid.NewGuid().ToString("N");
        Ticket ticket;
        await using (var first = new PostgresTicketStore(ConnectionString))
        {
            await first.InitializeAsync(default);
            await using var session = await first.LockAsync(default);
            ticket = await session.ReserveAsync("support", ulong.MaxValue, key, DateTimeOffset.UtcNow, default);
            ticket = ticket with { State = TicketState.Closed, ChannelId = BitConverter.ToUInt64(Guid.NewGuid().ToByteArray()),
                ClosedAt = DateTimeOffset.UtcNow, DeleteAfter = DateTimeOffset.UtcNow.AddDays(7) };
            await session.SaveAsync(ticket, "test-close", ulong.MaxValue, default);
            await session.GetOrCreateDeliveryAsync(key, "add-faction:123", default);
            await session.SetCooldownAsync(key, DateTimeOffset.UtcNow, default);
        }
        await using var second = new PostgresTicketStore(ConnectionString);
        await second.InitializeAsync(default);
        await using var resumed = await second.LockAsync(default);
        Assert.That(await resumed.FindInteractionAsync(key, default), Is.EqualTo(ticket));
        var intent = await resumed.GetOrCreateDeliveryAsync(key, "remove-faction:123", default);
        Assert.That(intent, Is.EqualTo(new DeliveryState("add-faction:123", false)));
        await resumed.CompleteDeliveryAsync(key, default);
        Assert.That((await resumed.GetOrCreateDeliveryAsync(key, "new", default)).Completed, Is.True);
        Assert.That(await resumed.GetCooldownAsync(key, default), Is.Not.Null);
    }

    [Test]
    public async Task CompetingStoreCannotMutateWhileAnotherSessionOwnsLock()
    {
        await using var first = new PostgresTicketStore(ConnectionString);
        await using var second = new PostgresTicketStore(ConnectionString);
        await first.InitializeAsync(default);
        var session = await first.LockAsync(default);
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        try
        {
            Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await using var competing = await second.LockAsync(timeout.Token);
            });
        }
        finally { await session.DisposeAsync(); }
        using var available = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await using var next = await second.LockAsync(available.Token);
        Assert.That(await next.GetTicketsAsync(available.Token), Is.Not.Null);
    }
}
