using System.Collections.Concurrent;
using System.Net;
using Discord.Net;
using SWLOR.DiscordBot.Core;

namespace SWLOR.DiscordBot.Hosting;

public sealed class ResponseDeletionQueue(TimeProvider clock)
{
    private readonly ConcurrentDictionary<(ulong Channel, ulong Message), (DateTimeOffset Due, int Attempts)> pending = new();
    public void Schedule(ulong channelId, ulong messageId, TimeSpan delay) => pending[(channelId, messageId)] = (clock.GetUtcNow() + delay, 0);
    public async Task DeleteDueAsync(ICommunityDiscord discord, CancellationToken ct)
    {
        foreach (var entry in pending.Where(x => x.Value.Due <= clock.GetUtcNow()))
        {
            if (!pending.TryRemove(entry.Key, out _)) continue;
            try { await discord.DeleteMessageAsync(entry.Key.Channel, entry.Key.Message, ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (HttpException ex) when (ex.HttpCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden) { }
            catch
            {
                if (entry.Value.Attempts < 2) pending[entry.Key] = (clock.GetUtcNow() + TimeSpan.FromMinutes(1), entry.Value.Attempts + 1);
            }
        }
    }
}
