using Nwn.Hosting.Bootstrap;
using Nwn.Hosting.Reload;
using SWLOR.Game.Host.Services;

namespace SWLOR.Game.Host.Bootstrap;

/// <summary>
/// SWLOR's <see cref="IHostMode"/>. It owns the <see cref="ReloadHost"/> that loads the game build as a generation and
/// hands the native callbacks to it: every run-script call goes to the active generation's dispatcher with its
/// conditional result (<see cref="ReloadHost.DispatchScriptResult"/>), every frame pumps the engine-thread queue and then
/// ticks the generation (inline, at the start of the main loop, as <see cref="HostOptions.Create"/> sets), and every
/// signal goes to the generation's bound signal handler. Reload stays off: the host loads one generation, at boot.
/// Every member runs on the engine thread.
/// </summary>
public sealed class SwlorHostMode : IHostMode
{
    /// <summary>The engine-thread operations one frame runs from the queue (none are queued while reload is off).</summary>
    private const int OperationsPerFrame = 8;

    /// <summary>How long the first load waits between pumps of the engine-thread queue while the game validates off-thread.</summary>
    private static readonly TimeSpan FirstLoadPollInterval = TimeSpan.FromMilliseconds(1);

    /// <summary>There is no running generation to drain on the first load.</summary>
    private static readonly TimeSpan FirstLoadDrainTimeout = TimeSpan.FromSeconds(1);

    private readonly CoreFunctionHandler _coreFunctions;
    private readonly HostSettings _settings;
    private readonly HostCallbacks _callbacks;
    private readonly HostServices _services;
    private readonly Action<string, string> _log;
    private NativeBindings? _bindings;
    private ReloadHost? _reloadHost;
    private Exception? _startFailure;
    private bool _destroyed;

    /// <param name="coreFunctions">The package's closure handler, which the reload host tracks closures in per generation.</param>
    /// <param name="settings">The host's options.</param>
    public SwlorHostMode(CoreFunctionHandler coreFunctions, HostSettings settings)
        : this(coreFunctions, settings, new HostRedisConnectionSource(() => settings.RedisHost), NativeHost.Log)
    {
    }

    /// <summary>For tests: the Redis connection source and the console log are replaced.</summary>
    public SwlorHostMode(
        CoreFunctionHandler coreFunctions,
        HostSettings settings,
        SWLOR.Game.Contracts.IRedisConnectionSource redisConnection,
        Action<string, string> log)
    {
        _coreFunctions = coreFunctions ?? throw new ArgumentNullException(nameof(coreFunctions));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        ArgumentNullException.ThrowIfNull(redisConnection);
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _callbacks = new HostCallbacks();
        _services = new HostServices()
            .Add<global::NWN.Core.ICoreFunctionHandler>(coreFunctions)
            .Add(redisConnection)
            .Add<SWLOR.Game.Contracts.IHostCallbacks>(_callbacks);
    }

    /// <summary>The frame and signal callbacks the generations see.</summary>
    public HostCallbacks Callbacks => _callbacks;

    /// <summary>The objects the generations receive through <c>IHostServices</c>.</summary>
    public HostServices Services => _services;

    /// <summary>The generation the host runs, once <see cref="LoadGame"/> has activated it.</summary>
    public long? ActiveGeneration { get; private set; }

    /// <inheritdoc/>
    public void Start()
    {
        _bindings = new NativeBindings(Environment.CurrentManagedThreadId);
        _reloadHost = new ReloadHost(_bindings, _services, HostOptions.LoadOptions(_settings), _coreFunctions);
    }

    /// <summary>
    /// Loads, validates and activates the game build as generation 1, on the engine thread, before NWNX's bootstrap call
    /// returns, so the engine finds every script handler registered when it runs the boot scripts, as with the native
    /// entry. The game validates off the engine thread; meanwhile this thread pumps the engine-thread queue, which is how
    /// the activation reaches it. Throws when the host did not start or the build did not activate: the server must not
    /// run without its game, and an exception out of the bootstrap stops it as a failed native boot does.
    /// </summary>
    public ReloadReport LoadGame()
    {
        if (_startFailure is not null)
            throw new InvalidOperationException("The SWLOR host failed to start.", _startFailure);
        if (_reloadHost is null || _bindings is null)
            throw new InvalidOperationException("The SWLOR host was not started.");

        _log("generation", $"loading {_settings.GameAssemblyPath} collectible={_settings.Collectible}");
        var load = _reloadHost.ReloadAsync(_settings.GameAssemblyPath, FirstLoadDrainTimeout).AsTask();
        while (!load.IsCompleted)
        {
            _bindings.Pump(int.MaxValue);
            if (!load.IsCompleted)
                Thread.Sleep(FirstLoadPollInterval);
        }

        var report = load.GetAwaiter().GetResult();
        if (report.Outcome != ReloadOutcome.Applied)
            throw new InvalidOperationException($"The game build did not activate ({report.Outcome}): {report.Detail}");

        ActiveGeneration = report.ActiveGeneration;
        _log("generation", $"active generation={report.ActiveGeneration} validate={report.CandidateValidation.TotalMilliseconds:F0}ms " +
                           $"activate={report.ApplyPause.TotalMilliseconds:F0}ms");
        return report;
    }

    /// <inheritdoc/>
    public bool BeforeFrame() => !_destroyed;

    /// <inheritdoc/>
    public void RunFrame(ulong frame)
    {
        _callbacks.BeginFrame(frame);
        _bindings?.Pump(OperationsPerFrame);
        _reloadHost?.PumpMainLoop();
    }

    /// <inheritdoc/>
    public bool OnRunScript(string script, uint objectSelf) => OnRunScriptResult(script, objectSelf) != NativeHost.ScriptNotHandled;

    /// <inheritdoc/>
    public int OnRunScriptResult(string script, uint objectSelf) =>
        _reloadHost?.DispatchScriptResult(script, objectSelf) ?? NativeHost.ScriptNotHandled;

    /// <inheritdoc/>
    public void OnSignal(string signal) => _callbacks.DeliverSignal(signal);

    /// <inheritdoc/>
    /// <remarks>
    /// The native entry does nothing on <c>ON_DESTROY_SERVER</c> (the game's <c>app_shutdown</c> handlers run on process
    /// exit), so neither does the host: the closure handler is not disposed, because game code that schedules a closure
    /// during the engine's teardown must not throw where the native entry's handler only asked NWNX.
    /// </remarks>
    public void OnDestroyServer()
    {
        _destroyed = true;
        _log("destroy", "server destroyed; frames stop");
    }

    /// <inheritdoc/>
    public void OnCallbackFailure(string phase, Exception exception)
    {
        if (phase == "bootstrap")
            _startFailure ??= exception;
        _log("failure", $"{phase}: {exception}");
    }

    /// <inheritdoc/>
    public void OnFrameScheduleFailure(Exception exception) => _log("failure", $"main-loop-schedule: {exception}");
}
