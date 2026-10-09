namespace SWLOR.DiscordBot.Discord;

internal sealed class GatewaySessionState
{
    private readonly object sync = new();
    private int generation;
    private bool seenReady;
    private bool validated;
    private bool pending;
    private bool ready;
    internal event Action? ReadinessEstablished;

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
        }
        if (connected) ReadinessEstablished?.Invoke();
        return connected;
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
        bool becameReady;
        lock (sync)
        {
            if (!connected || !seenReady || pending) return null;
            if (!validated)
            {
                pending = true;
                ready = false;
                return ++generation;
            }
            becameReady = !ready;
            ready = true;
        }
        if (becameReady) ReadinessEstablished?.Invoke();
        return null;
    }
    public void Stop()
    {
        lock (sync) { generation++; seenReady = false; pending = false; validated = false; ready = false; }
    }
}
