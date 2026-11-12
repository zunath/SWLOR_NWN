using System.Diagnostics;
using System.Globalization;
using Nwn.Hosting.Bootstrap;
using Nwn.Hosting.Reload;
using SWLOR.Game.Contracts;
using SWLOR.Game.Host.Reload;
using SWLOR.Game.Host.Services;
using SWLOR.Game.NativeHooks;

namespace SWLOR.Game.Host.Bootstrap;

/// <summary>
/// SWLOR's <see cref="IHostMode"/>. It owns the <see cref="ReloadHost"/> that loads the game build as a generation and
/// hands the native callbacks to it: every run-script call goes to the active generation's dispatcher with its
/// conditional result (<see cref="ReloadHost.DispatchScriptResult"/>), every frame pumps the engine-thread queue and then
/// ticks the generation (inline, at the start of the main loop, as <see cref="HostOptions.Create"/> sets), and every
/// signal goes to the generation's bound signal handler. The host loads generation 1 at boot. With hot reload on
/// (<see cref="HostSettings.HotReload"/>) it also watches for reload signals once the module has loaded and replaces the
/// running build with a candidate (<see cref="ReloadAsync"/>), holding the engine thread inside one frame from the old
/// build's quiescence to the new build's activation. Every member runs on the engine thread except
/// <see cref="ReloadAsync"/>.
/// </summary>
public sealed class SwlorHostMode : IHostMode
{
    /// <summary>The engine-thread operations one frame runs from the queue (none are queued unless a reload runs).</summary>
    private const int OperationsPerFrame = 8;

    /// <summary>How long the engine thread waits between pumps of the engine-thread queue while a load or a reload works off-thread.</summary>
    private static readonly TimeSpan PumpPollInterval = TimeSpan.FromMilliseconds(1);

    /// <summary>There is no running generation to drain on the first load.</summary>
    private static readonly TimeSpan FirstLoadDrainTimeout = TimeSpan.FromSeconds(1);

    /// <summary>How long a retired build may take to drain its callbacks on a reload; the engine thread is held meanwhile.</summary>
    private static readonly TimeSpan ReloadDrainTimeout = TimeSpan.FromSeconds(10);

    /// <summary>How long a reload waits for the engine thread to leave its hold before it reports without the pause.</summary>
    private static readonly TimeSpan PauseReleaseTimeout = TimeSpan.FromSeconds(30);

    private readonly CoreFunctionHandler _coreFunctions;
    private readonly ReloadSchemaGate _schemaGate;
    private readonly HostSettings _settings;
    private readonly HostCallbacks _callbacks;
    private readonly HostLifecycle _lifecycle;
    private readonly HostServices _services;
    private readonly Action<string, string> _log;
    private readonly TimeSpan _reloadPollInterval;
    private NativeBindings? _bindings;
    private ReloadHost? _reloadHost;
    private ReloadTrigger? _trigger;
    private ReloadPause? _pause;
    private long _activeGeneration;
    private Exception? _startFailure;
    private bool _destroyed;

    /// <param name="coreFunctions">The package's closure handler, which the reload host tracks closures in per generation.</param>
    /// <param name="settings">The host's options.</param>
    /// <param name="hooks">The process's native hooks, which the game enables and binds its sinks to.</param>
    public SwlorHostMode(CoreFunctionHandler coreFunctions, HostSettings settings, NativeHookTable hooks)
        : this(coreFunctions, settings, new HostRedisConnectionSource(() => settings.RedisHost), hooks, NativeHost.Log)
    {
    }

    /// <summary>For tests: the Redis connection source and the console log are replaced.</summary>
    public SwlorHostMode(
        CoreFunctionHandler coreFunctions,
        HostSettings settings,
        IRedisConnectionSource redisConnection,
        NativeHookTable hooks,
        Action<string, string> log)
        : this(coreFunctions, settings, redisConnection, hooks, log, ReloadTrigger.DefaultPollInterval)
    {
    }

    /// <summary>For tests: also how often the reload trigger looks at the signal file.</summary>
    public SwlorHostMode(
        CoreFunctionHandler coreFunctions,
        HostSettings settings,
        IRedisConnectionSource redisConnection,
        NativeHookTable hooks,
        Action<string, string> log,
        TimeSpan reloadPollInterval)
        : this(coreFunctions, settings, redisConnection, hooks, log, reloadPollInterval, new ReloadSchemaGate(redisConnection ?? throw new ArgumentNullException(nameof(redisConnection))))
    {
    }

    /// <summary>For tests: also the reload's schema gate (by default it reads the database's version through <paramref name="redisConnection"/>).</summary>
    public SwlorHostMode(
        CoreFunctionHandler coreFunctions,
        HostSettings settings,
        IRedisConnectionSource redisConnection,
        NativeHookTable hooks,
        Action<string, string> log,
        TimeSpan reloadPollInterval,
        ReloadSchemaGate schemaGate)
    {
        _coreFunctions = coreFunctions ?? throw new ArgumentNullException(nameof(coreFunctions));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        ArgumentNullException.ThrowIfNull(redisConnection);
        ArgumentNullException.ThrowIfNull(hooks);
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _schemaGate = schemaGate ?? throw new ArgumentNullException(nameof(schemaGate));
        _reloadPollInterval = reloadPollInterval;
        _callbacks = new HostCallbacks();
        _lifecycle = new HostLifecycle(settings);
        _services = new HostServices()
            .Add<global::NWN.Core.ICoreFunctionHandler>(coreFunctions)
            .Add(redisConnection)
            .Add<IHostCallbacks>(_callbacks)
            .Add<IHookControl>(hooks)
            .Add<INativeSinkBinding>(hooks)
            .Add<IHostLifecycle>(_lifecycle);
    }

    /// <summary>The frame and signal callbacks the generations see.</summary>
    public HostCallbacks Callbacks => _callbacks;

    /// <summary>What the generations read about the process: the module load, the environment, the reload switch.</summary>
    public HostLifecycle Lifecycle => _lifecycle;

    /// <summary>The objects the generations receive through <c>IHostServices</c>.</summary>
    public HostServices Services => _services;

    /// <summary>The generation the host runs: set when <see cref="LoadGame"/> or a reload has activated one, cleared by a reload that needs a restart.</summary>
    public long? ActiveGeneration => Interlocked.Read(ref _activeGeneration) is var generation and not 0 ? generation : null;

    /// <summary>The reload trigger, when hot reload is on.</summary>
    public ReloadTrigger? Trigger => _trigger;

    /// <inheritdoc/>
    /// <remarks>
    /// With hot reload on, creates the reload trigger, which starts watching once the module has loaded. A switch the host
    /// refuses (<see cref="HostSettings.HotReloadRefusal"/>) is logged and reload stays off.
    /// </remarks>
    public void Start()
    {
        _bindings = new NativeBindings(Environment.CurrentManagedThreadId);
        _reloadHost = new ReloadHost(_bindings, _services, HostOptions.LoadOptions(_settings), _coreFunctions);

        if (_settings.HotReload)
        {
            _trigger = new ReloadTrigger(new ReloadLayout(_settings.GenerationRoot, _settings.ReloadDirectory), ReloadAsync, _log, _reloadPollInterval);
            _log("reload-on", $"{HostSettings.HotReloadVariable} is on ({_settings.EnvironmentName}): reload signals are watched once the module has loaded.");
        }
        else if (_settings.HotReloadRefusal is { } refusal)
        {
            _log("reload-refused", refusal + " Reload stays off.");
        }
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
                Thread.Sleep(PumpPollInterval);
        }

        var report = load.GetAwaiter().GetResult();
        if (report.Outcome != ReloadOutcome.Applied)
            throw new InvalidOperationException($"The game build did not activate ({report.Outcome}): {report.Detail}");

        Interlocked.Exchange(ref _activeGeneration, report.ActiveGeneration ?? 0);
        _log("generation", $"active generation={report.ActiveGeneration} validate={report.CandidateValidation.TotalMilliseconds:F0}ms " +
                           $"activate={report.ApplyPause.TotalMilliseconds:F0}ms");
        return report;
    }

    /// <summary>
    /// Replaces the running build with the candidate assembly at <paramref name="candidatePath"/>, through the same
    /// <see cref="ReloadHost"/> that loaded generation 1. Called off the engine thread (the reload trigger). The candidate
    /// loads and validates off the engine thread while the running build keeps serving; a rejection leaves it untouched.
    /// Once the candidate has passed validation the next frame does not end until the reload has: the engine thread pumps
    /// the old build's quiescence, its dispose and the new build's activation, so no engine frame runs without an active
    /// build (no script reaches the game unhandled, no hook fires without a sink). A failure after quiescence leaves the
    /// host in <see cref="ReloadOutcome.RestartRequired"/>: no game build runs until the server is restarted.
    /// </summary>
    public async Task<ReloadAttempt> ReloadAsync(string candidatePath, CancellationToken cancellationToken)
    {
        var reloadHost = _reloadHost ?? throw new InvalidOperationException("The SWLOR host was not started.");

        _log("reload-start", $"validating {candidatePath} off the engine thread; generation {ActiveGeneration} keeps running");
        var pause = new ReloadPause();
        ReloadReport report;
        try
        {
            report = await reloadHost.ReloadAsync(candidatePath, ReloadDrainTimeout,
                    (requiredSchemaVersion, token) => GateAndArmPauseAsync(pause, requiredSchemaVersion, token), cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            pause.Finish();
        }

        if (pause.Armed && !await LeftHoldAsync(pause).ConfigureAwait(false))
            _log("reload-pause", "the engine thread has not left its hold; the receipt carries no pause");

        if (report.Outcome == ReloadOutcome.Applied)
        {
            Interlocked.Exchange(ref _activeGeneration, report.ActiveGeneration ?? 0);
        }
        else if (report.Outcome == ReloadOutcome.RestartRequired)
        {
            Interlocked.Exchange(ref _activeGeneration, 0);
            _log("restart-required", $"the reload failed after generation {report.PreviousGeneration} was quiesced: no game build runs " +
                                     $"and the server needs a restart. {report.Detail}");
        }

        return new ReloadAttempt(report, pause.Held, pause.Frame);
    }

    /// <inheritdoc/>
    public bool BeforeFrame() => !_destroyed;

    /// <inheritdoc/>
    /// <remarks>
    /// While a reload that has passed validation runs, the frame holds the engine thread (<see cref="HoldForReload"/>) and
    /// ticks the new build once it is active. Otherwise the reload check is one field read.
    /// </remarks>
    public void RunFrame(ulong frame)
    {
        // The hold is timed from the start of this frame: the pump below can already run the reload's engine-thread work
        // (the old build's quiescence) once the hold is armed, and the engine is blocked for that too.
        var frameStart = Stopwatch.GetTimestamp();
        _callbacks.BeginFrame(frame);
        _bindings?.Pump(OperationsPerFrame);
        var pause = Volatile.Read(ref _pause);
        if (pause is not null)
            HoldForReload(pause, frame, frameStart);
        _reloadHost?.PumpMainLoop();
    }

    /// <inheritdoc/>
    public bool OnRunScript(string script, uint objectSelf) => OnRunScriptResult(script, objectSelf) != NativeHost.ScriptNotHandled;

    /// <inheritdoc/>
    /// <remarks>
    /// While a generation activating by a reload has bound its dispatcher (<see cref="IHostCallbacks.BindActivationScripts"/>),
    /// the package has no active generation, and every script goes to that dispatcher: the engine runs it from inside the
    /// new build's reload bootstrap, as it runs it from a boot handler at boot.
    /// </remarks>
    public int OnRunScriptResult(string script, uint objectSelf)
    {
        if (_callbacks.ActivationScripts is { } activating)
            return DispatchToActivatingGeneration(activating, script, objectSelf);

        return _reloadHost?.DispatchScriptResult(script, objectSelf) ?? NativeHost.ScriptNotHandled;
    }

    /// <summary>The package's dispatch rules for the activating generation: its conditional result, and a throw is unhandled.</summary>
    private int DispatchToActivatingGeneration(Nwn.Hosting.Contracts.Generations.IScriptDispatcher dispatcher, string script, uint objectSelf)
    {
        try
        {
            if (!dispatcher.TryHandle(script, objectSelf, out var conditionalResult))
                return NativeHost.ScriptNotHandled;

            return conditionalResult ? NativeHost.ScriptConditionTrue : NativeHost.ScriptHandled;
        }
        catch (Exception exception)
        {
            _log("script-dispatch-failure", $"{script} (activating generation): {exception}");
            return NativeHost.ScriptNotHandled;
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <c>ON_MODULE_LOAD_FINISH</c>, after the active generation has taken it, marks the module loaded
    /// (<see cref="IHostLifecycle.ModuleLoaded"/>) and starts the reload trigger when reload is on.
    /// </remarks>
    public void OnSignal(string signal)
    {
        _callbacks.DeliverSignal(signal);
        if (signal == HostLifecycle.ModuleLoadFinishSignal && _lifecycle.MarkModuleLoaded())
            _trigger?.Start();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The native entry does nothing on <c>ON_DESTROY_SERVER</c> (the game's <c>app_shutdown</c> handlers run on process
    /// exit), so neither does the host: the closure handler is not disposed, because game code that schedules a closure
    /// during the engine's teardown must not throw where the native entry's handler only asked NWNX. The reload trigger
    /// stops watching.
    /// </remarks>
    public void OnDestroyServer()
    {
        _destroyed = true;
        var trigger = _trigger;
        _trigger = null;
        if (trigger is not null)
            _ = trigger.DisposeAsync().AsTask();
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

    /// <summary>
    /// The reload's schema gate, called off the engine thread once the candidate has passed validation and right before the
    /// running build is quiesced. A candidate that needs a server migration (<see cref="ReloadSchemaGate"/>) is rejected and the
    /// running build keeps serving; otherwise the hold is armed, so the next frame (or the one running) keeps the engine thread
    /// until the reload ends.
    /// </summary>
    private async ValueTask<ReloadSchemaGateResult> GateAndArmPauseAsync(ReloadPause pause, int requiredSchemaVersion, CancellationToken cancellationToken)
    {
        var gate = await _schemaGate.CheckAsync(requiredSchemaVersion, cancellationToken).ConfigureAwait(false);
        _log("reload-schema", gate.Detail);
        if (!gate.Compatible)
            return gate;

        pause.Arm();
        Volatile.Write(ref _pause, pause);
        return gate;
    }

    /// <summary>
    /// Keeps the engine thread in this frame until the reload ends, running its engine-thread work (the old build's
    /// quiescence and dispose, the new build's activation) as it arrives.
    /// </summary>
    private void HoldForReload(ReloadPause pause, ulong frame, long frameStart)
    {
        while (!pause.Finished)
        {
            _bindings!.Pump(int.MaxValue);
            if (!pause.Finished)
                Thread.Sleep(PumpPollInterval);
        }

        var held = Stopwatch.GetElapsedTime(frameStart);
        Interlocked.CompareExchange(ref _pause, null, pause);
        pause.Release(held, frame);
        _log("reload-pause", string.Create(CultureInfo.InvariantCulture,
            $"frame={frame} heldMs={held.TotalMilliseconds:F0}: the engine thread stayed in this frame from the old build's quiescence to the end of the reload"));
    }

    private static async Task<bool> LeftHoldAsync(ReloadPause pause)
    {
        try
        {
            await pause.Released.WaitAsync(PauseReleaseTimeout).ConfigureAwait(false);
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }
}
