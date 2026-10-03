namespace SWLOR.DiscordBot.Discord;

internal sealed class GatewaySessionState
{
    private readonly object sync = new();
    private int generation;
    private bool seenReady;
    private bool validated;
    private bool pending;
    private bool ready;

    public bool IsReady(bool connected) { lock (sync) return connected && ready && validated && !pending; }
    public bool IsCurrent(int token) { lock (sync) return token == generation && pending; }
    public int BeginValidation()
    {
        lock (sync)
        {
            seenReady = true;
            validated = false;
            ready = false;
            pending = true;
            return ++generation;
        }
    }
    public bool CompleteValidation(int token, bool connected)
    {
        lock (sync)
        {
            if (token != generation || !pending) return false;
            pending = false;
            validated = connected;
            ready = connected;
            return ready;
        }
    }
    public void AbandonValidation(int token)
    {
        lock (sync) { if (token == generation) { pending = false; ready = false; } }
    }
    public void Disconnect()
    {
        lock (sync) { generation++; pending = false; ready = false; }
    }
    public int? ObserveHeartbeat(bool connected)
    {
        lock (sync)
        {
            if (!connected || !seenReady || pending) return null;
            if (validated) { ready = true; return null; }
            pending = true;
            ready = false;
            return ++generation;
        }
    }
    public void Stop()
    {
        lock (sync) { generation++; seenReady = false; pending = false; validated = false; ready = false; }
    }
}
