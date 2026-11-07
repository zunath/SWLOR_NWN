using StackExchange.Redis;
using SWLOR.Game.Server.Domain.Core;

namespace SWLOR.Game.Server.IntegrationTests.Support;

/// <summary>
/// The real <see cref="DBService"/> against real Redis. Only the three seams that reach the game server are replaced:
/// the end-of-frame subscription (the test ends frames itself, <see cref="EndFrame"/>), the <c>db_loaded</c> script, and,
/// for a database in another container, the endpoint.
/// </summary>
public sealed class IntegrationDBService : DBService
{
    private readonly string? _endpoint;
    private Action? _scriptContextEnd;

    /// <param name="endpoint">The Redis to connect to instead of <c>NWNX_REDIS_HOST</c>, as "host:port".</param>
    public IntegrationDBService(string? endpoint = null)
    {
        _endpoint = endpoint;
    }

    /// <summary>
    /// Ends the script context the way the server does at the end of every main loop: the entities cached during the
    /// frame are dropped, so the next <c>Get</c> reads Redis.
    /// </summary>
    public void EndFrame() => _scriptContextEnd?.Invoke();

    protected override IConnectionMultiplexer Connect(ConfigurationOptions options)
    {
        if (_endpoint != null)
        {
            options.EndPoints.Clear();
            options.EndPoints.Add(_endpoint);
        }

        return base.Connect(options);
    }

    protected override void SubscribeScriptContextEnd(Action handler) => _scriptContextEnd = handler;

    protected override void ExecuteDatabaseLoadedScript()
    {
    }
}
