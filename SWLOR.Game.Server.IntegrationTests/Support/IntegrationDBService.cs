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

    /// <summary>
    /// Drops the JSON the service keeps across frames for types marked <c>JsonCachedAcrossFrames</c>. The server only
    /// changes those records through the service, which keeps the cache current; the tier also deletes keys behind its back
    /// between tests, so it clears the cache then.
    /// </summary>
    public void ForgetCrossFrameJson()
    {
        var field = typeof(DBService).GetField("_jsonAcrossFrames", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?? throw new InvalidOperationException("DBService no longer has the cross-frame JSON cache this tier clears.");
        ((System.Collections.IDictionary)field.GetValue(this)!).Clear();
    }

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
