using System.Net;
using Discord.Net;
using Microsoft.Extensions.Logging;
using SWLOR.DiscordBot.Core;

namespace SWLOR.DiscordBot.Hosting;

public sealed class ResponseDeletionQueue(IResponseDeletionStore store, TimeProvider clock,
    ILogger<ResponseDeletionQueue> logger)
{
    public Task ScheduleAsync(ulong channelId, ulong messageId, TimeSpan delay, CancellationToken ct) =>
        store.ScheduleDeletionAsync(channelId, messageId, clock.GetUtcNow() + delay, ct);

    public async Task DeleteDueAsync(ICommunityDiscord discord, CancellationToken ct)
    {
        // The active-worker lease ensures a single consumer. These short DB operations hold no ticket
        // or community advisory lock while Discord is rate limited or unavailable.
        foreach (var deletion in await store.GetDueDeletionsAsync(clock.GetUtcNow(), ct))
        {
            ct.ThrowIfCancellationRequested();
            try { await discord.DeleteMessageAsync(deletion.ChannelId, deletion.MessageId, ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.NotFound)
            {
                await store.CompleteDeletionAsync(deletion.ChannelId, deletion.MessageId, ct);
                continue;
            }
            catch (Exception ex)
            {
                var forbidden = ex is HttpException { HttpCode: HttpStatusCode.Forbidden };
                var error = forbidden ? "Forbidden: restore channel access and message deletion permissions." : ex.GetType().Name;
                // Retain failed intentions indefinitely, with bounded backoff so lost permissions are
                // actionable and recovery does not require replaying the original command.
                var delay = TimeSpan.FromMinutes(Math.Min(60, Math.Pow(2, Math.Min(deletion.Attempts, 6))));
                await store.RetryDeletionAsync(deletion, clock.GetUtcNow() + delay, error, ct);
                logger.LogWarning("Response deletion pending for channel {ChannelId}, message {MessageId}: {ErrorKind}",
                    deletion.ChannelId, deletion.MessageId, error);
                continue;
            }
            // Cancellation or a DB failure leaves the original intent pending; repeating a delete is safe.
            await store.CompleteDeletionAsync(deletion.ChannelId, deletion.MessageId, ct);
        }
    }
}
