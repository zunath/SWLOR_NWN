using System.Threading.Channels;

namespace SWLOR.DiscordBot.Hosting;

internal sealed class TicketMaintenanceScheduler
{
    private readonly Channel<byte> requests = Channel.CreateBounded<byte>(new BoundedChannelOptions(1)
    { FullMode = BoundedChannelFullMode.DropWrite, SingleReader = true, SingleWriter = false });

    public void RequestSweep() => requests.Writer.TryWrite(0);

    public async Task RunAsync(TimeSpan interval, TimeProvider clock, Func<bool> isReady,
        Func<CancellationToken, Task> sweep, CancellationToken ct)
    {
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var token = lifetime.Token;
        using var timer = new PeriodicTimer(interval, clock);
        RequestSweep();
        var nextTick = timer.WaitForNextTickAsync(token).AsTask();
        var nextRequest = requests.Reader.WaitToReadAsync(token).AsTask();
        try
        {
            while (true)
            {
                await Task.WhenAny(nextTick, nextRequest).WaitAsync(token);
                token.ThrowIfCancellationRequested();
                if (nextTick.IsCompleted)
                {
                    if (!await nextTick) return;
                    nextTick = timer.WaitForNextTickAsync(token).AsTask();
                }
                if (nextRequest.IsCompleted)
                {
                    if (!await nextRequest) return;
                    while (requests.Reader.TryRead(out _)) { }
                    nextRequest = requests.Reader.WaitToReadAsync(token).AsTask();
                }
                // Ready notifications and timer ticks share one consumer, so sweeps never overlap.
                if (isReady()) await sweep(token);
            }
        }
        finally
        {
            await lifetime.CancelAsync();
            timer.Dispose();
            try { await Task.WhenAll(nextTick, nextRequest); }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        }
    }
}
