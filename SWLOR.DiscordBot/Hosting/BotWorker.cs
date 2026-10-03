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
        Task? archiveExpiration = null;
        Task? responseCleanup = null;
        Task? deliveryRetention = null;
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
            var maintenanceErrors = await ValidatePersistedConfigurationAsync(configuration, store, ct);
            if (maintenanceErrors.Count > 0)
            {
                foreach (var error in maintenanceErrors) logger.LogCritical("Ticket maintenance configuration invalid: {ConfigurationError}", error);
                throw new InvalidOperationException("Persisted ticket maintenance requires retained configuration.");
            }
            deliveryRetention = PruneDeliveriesAsync(ct);
            gateway.Attach(ct);
            consumers = Enumerable.Range(0, 4).Select(_ => gateway.ProcessAsync(ct)).ToArray();
            await client.LoginAsync(TokenType.Bot, secrets.Token);
            await Program.StartGatewayAsync(configuration,
                async () => (await ((IDiscordClient)client.Rest).GetApplicationInfoAsync(DiscordOperations.Options(ct))).Flags,
                () => client.StartAsync());
            maintenance = MaintainAsync(ct);
            archiveExpiration = ExpireArchivesAsync(ct);
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
            try { await Task.WhenAll(consumers.Concat(new[] { maintenance, archiveExpiration, responseCleanup, deliveryRetention }.OfType<Task>())); }
            catch (OperationCanceledException) { }
            disposingLease = true;
            if (lease is not null) await lease.DisposeAsync();
        }
    }
    internal static async Task<IReadOnlyList<string>> ValidatePersistedConfigurationAsync(BotConfiguration config, ITicketStore store, CancellationToken ct)
    {
        await using var session = await store.LockAsync(ct);
        return ConfigurationValidator.ValidatePersistedTickets(config, await session.GetTicketsAsync(ct));
    }

    private async Task PruneDeliveriesAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5), clock);
        do
        {
            // Local database retention continues during Discord outages and when ticketing is disabled.
            try { await store.PruneCompletedDeliveriesAsync(clock.GetUtcNow(), ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }
            catch (Exception ex)
            {
                logger.LogWarning("Completed delivery retention failed; cleanup will retry: {ErrorKind}.", DiscordGateway.SafeError(ex));
            }
        } while (await timer.WaitForNextTickAsync(ct));
    }

    private async Task DeleteResponsesAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15), clock);
        do
        {
            if (!gateway.Ready) continue;
            try { await deletions.DeleteDueAsync(community, ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }
            catch (Exception ex)
            {
                logger.LogWarning("Response cleanup failed; pending deletions will retry: {ErrorKind}.", DiscordGateway.SafeError(ex));
            }
        } while (await timer.WaitForNextTickAsync(ct));
    }
    private async Task MaintainAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(MaintenanceInterval, clock);
        do
        {
            if (!gateway.Ready) continue;
            try { await RetryMaintenanceAsync("Ticket maintenance", tickets.MaintainAsync, ct, boundWholePass: false); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }
            catch (Exception ex)
            {
                logger.LogWarning("Ticket maintenance failed after retries: {ErrorKind}.", DiscordGateway.SafeError(ex));
            }
        } while (await timer.WaitForNextTickAsync(ct));
    }

    private async Task ExpireArchivesAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(MaintenanceInterval, clock);
        do
        {
            try { await RetryMaintenanceAsync("Archive expiration", tickets.ExpireArchivesAsync, ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }
            catch (Exception ex)
            {
                logger.LogWarning("Archive expiration failed after retries: {ErrorKind}.", DiscordGateway.SafeError(ex));
            }
        } while (await timer.WaitForNextTickAsync(ct));
    }

    private TimeSpan MaintenanceInterval => configuration.Tickets.Enabled
        ? configuration.Tickets.CleanupInterval
        : TimeSpan.FromMinutes(5);

    private async Task RetryMaintenanceAsync(string operationName, Func<CancellationToken, Task> operation, CancellationToken ct, bool boundWholePass = true)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            using var operationTimeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            // Per-ticket inactivity and per-request network limits cover progressing transcript exports.
            if (boundWholePass) operationTimeout.CancelAfter(TimeSpan.FromMinutes(10));
            try { await operation(operationTimeout.Token); return; }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                logger.LogWarning("{Operation} attempt {Attempt} failed: {ErrorKind}.",
                    operationName, attempt + 1, DiscordGateway.SafeError(ex));
                if (attempt < 2) await Task.Delay(TimeSpan.FromSeconds(5 * (attempt + 1)), clock, ct);
            }
        }
    }
}
