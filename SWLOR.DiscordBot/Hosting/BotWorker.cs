using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using SWLOR.DiscordBot.Configuration;
using SWLOR.DiscordBot.Core;
using SWLOR.DiscordBot.Discord;

namespace SWLOR.DiscordBot.Hosting;

public sealed class BotWorker(BotConfiguration configuration, BotSecrets secrets, DiscordSocketClient client,
    DiscordGateway gateway, ITicketStore store, TicketService tickets, ICommunityDiscord community,
    ResponseDeletionQueue deletions, ReadinessMarker marker, TimeProvider clock, IHostApplicationLifetime lifetime,
    ILogger<BotWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        marker.Clear();
        using var shutdown = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var ct = shutdown.Token;
        NpgsqlConnection? lease = null;
        Task[] consumers = [];
        Task? maintenance = null;
        Task? responseCleanup = null;
        var disposingLease = false;
        try
        {
            var leaseSettings = new NpgsqlConnectionStringBuilder(secrets.Database) { Pooling = false, KeepAlive = 10 };
            lease = new NpgsqlConnection(leaseSettings.ConnectionString);
            await lease.OpenAsync(ct);
            lease.StateChange += (_, change) =>
            {
                if (disposingLease || ct.IsCancellationRequested || change.CurrentState == System.Data.ConnectionState.Open) return;
                gateway.Suspend();
                shutdown.Cancel();
                Environment.ExitCode = 1;
                logger.LogCritical("The active-worker database lease was lost.");
                lifetime.StopApplication();
            };
            await using (var command = new NpgsqlCommand("SELECT pg_try_advisory_lock(hashtextextended(@name, 0))", lease))
            {
                command.Parameters.AddWithValue("name", $"SWLOR.DiscordBot.worker:{configuration.GuildId}");
                if (await command.ExecuteScalarAsync(ct) is not true)
                    throw new InvalidOperationException("An active worker already owns this guild.");
            }
            await store.InitializeAsync(ct);
            gateway.Attach(ct);
            consumers = Enumerable.Range(0, 4).Select(_ => gateway.ProcessAsync(ct)).ToArray();
            await client.LoginAsync(TokenType.Bot, secrets.Token);
            await client.StartAsync();
            maintenance = MaintainAsync(ct);
            responseCleanup = DeleteResponsesAsync(ct);
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15), clock);
            do
            {
                // A dedicated session owns the worker lease. Losing it terminates this worker before further gateway work.
                await using var check = new NpgsqlCommand("SELECT 1", lease) { CommandTimeout = 10 };
                await check.ExecuteScalarAsync(ct);
                await marker.RefreshAsync(gateway.Ready, ct);

            } while (await timer.WaitForNextTickAsync(ct));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex)
        {
            logger.LogCritical("Bot worker stopped: {ErrorKind}.", DiscordGateway.SafeError(ex));
            Environment.ExitCode = 1;
            lifetime.StopApplication();
        }
        finally
        {
            marker.Clear();
            await shutdown.CancelAsync();
            gateway.Detach();
            try { await client.StopAsync(); await client.LogoutAsync(); }
            catch (Exception ex) { logger.LogWarning("Discord shutdown failed: {ErrorKind}.", DiscordGateway.SafeError(ex)); }
            try { await Task.WhenAll(consumers.Concat(new[] { maintenance, responseCleanup }.OfType<Task>())); }
            catch (OperationCanceledException) { }
            disposingLease = true;
            if (lease is not null) await lease.DisposeAsync();
        }
    }
    private async Task DeleteResponsesAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15), clock);
        do
        {
            if (gateway.Ready) await deletions.DeleteDueAsync(community, ct);
        } while (await timer.WaitForNextTickAsync(ct));
    }
    private async Task MaintainAsync(CancellationToken ct)
    {
        var interval = configuration.Tickets.Enabled ? configuration.Tickets.CleanupInterval : TimeSpan.FromMinutes(5);
        using var timer = new PeriodicTimer(interval, clock);
        do
        {
            if (!gateway.Ready || !configuration.Tickets.Enabled) continue;
            for (var attempt = 0; attempt < 3; attempt++)
            {
                using var operation = CancellationTokenSource.CreateLinkedTokenSource(ct);
                operation.CancelAfter(TimeSpan.FromMinutes(10));
                try { await tickets.MaintainAsync(operation.Token); break; }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }
                catch (Exception ex)
                {
                    logger.LogWarning("Ticket maintenance attempt {Attempt} failed: {ErrorKind}.", attempt + 1, DiscordGateway.SafeError(ex));
                    if (attempt < 2) await Task.Delay(TimeSpan.FromSeconds(5 * (attempt + 1)), clock, ct);
                }
            }
        } while (await timer.WaitForNextTickAsync(ct));
    }
}
