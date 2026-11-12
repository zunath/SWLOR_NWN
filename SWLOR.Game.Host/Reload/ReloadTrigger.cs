using Nwn.Hosting.Development;

namespace SWLOR.Game.Host.Reload;

/// <summary>
/// The development reload trigger: the package's <see cref="GameReloadSignalWatcher"/> polling the signal file and a
/// <see cref="ReloadSignalHandler"/> answering it. The host creates one only when hot reload is on, and starts it once the
/// module has loaded, so a reload never meets the engine's boot scripts; with reload off there is no watcher at all.
/// What the signal file holds when the watcher starts is never acted on: only a signal written while the server watches
/// reloads it.
/// </summary>
public sealed class ReloadTrigger : IAsyncDisposable
{
    /// <summary>How often the watcher looks at the signal file.</summary>
    public static readonly TimeSpan DefaultPollInterval = TimeSpan.FromSeconds(1);

    private readonly ReloadLayout _layout;
    private readonly ReloadSignalHandler _handler;
    private readonly TimeSpan _pollInterval;
    private readonly Action<string, string> _log;
    private GameReloadSignalWatcher? _watcher;

    /// <param name="layout">Where the signal, the candidates and the receipts are.</param>
    /// <param name="reloadAsync">Replaces the running build with the candidate assembly at the given path.</param>
    /// <param name="log">The server log: a category and a message.</param>
    /// <param name="pollInterval">How often the signal file is looked at; <see cref="DefaultPollInterval"/> on a server.</param>
    public ReloadTrigger(
        ReloadLayout layout,
        Func<string, CancellationToken, Task<ReloadAttempt>> reloadAsync,
        Action<string, string> log,
        TimeSpan pollInterval)
    {
        _layout = layout ?? throw new ArgumentNullException(nameof(layout));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(pollInterval, TimeSpan.Zero);
        _pollInterval = pollInterval;
        _handler = new ReloadSignalHandler(layout, reloadAsync, log);
    }

    /// <summary>True once <see cref="Start"/> has started the watcher.</summary>
    public bool Watching => _watcher is not null;

    /// <summary>
    /// Creates the reload folder and starts watching the signal file. Does nothing the second time. A folder the server
    /// cannot create is logged and reload stays off: the running build is never at stake.
    /// </summary>
    public void Start()
    {
        if (_watcher is not null)
            return;

        try
        {
            Directory.CreateDirectory(_layout.ReceiptsDirectory);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _log("reload-off", $"cannot create {_layout.ReceiptsDirectory}: {exception.Message}");
            return;
        }

        _watcher = new GameReloadSignalWatcher(
            _layout.SignalPath,
            _pollInterval,
            _handler.HandleAsync,
            exception => _log("reload-signal-failure", $"{exception.GetType().Name}: {exception.Message}"));
        _log("reload-watching", $"signal={_layout.SignalPath} candidates={_layout.GenerationsDirectory} receipts={_layout.ReceiptsDirectory}");
    }

    /// <summary>Stops watching. The task completes once a reload already running has ended.</summary>
    public ValueTask DisposeAsync()
    {
        var watcher = _watcher;
        _watcher = null;
        return watcher?.DisposeAsync() ?? ValueTask.CompletedTask;
    }
}
