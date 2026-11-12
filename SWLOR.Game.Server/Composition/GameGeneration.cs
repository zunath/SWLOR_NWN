using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nwn.Hosting.Contracts.Generations;
using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.NativeSinks;

namespace SWLOR.Game.Server.Composition
{
    /// <summary>
    /// The game build as a generation of the permanent host (<c>SWLOR.Game.Host</c>, MaintainabilityRefactorHotReload.md
    /// section 3, "A generation"). The host loads this assembly, finds this class (the only
    /// <see cref="IGameplayGeneration"/> in it), validates it off the engine thread and activates it on the engine thread.
    /// Generation 1 activates while NWNX's bootstrap call is still running; the engine then runs the boot scripts as it
    /// does for the native entry (<c>ServerManager.Bootstrap</c>). A generation that activates after the module has loaded
    /// (a hot reload on a development or test server) rebuilds the service container and the script dispatch, then runs the
    /// reload bootstrap: the engine runs no boot script for it, so it reruns the rerun boot handlers and adopts the live world
    /// (<see cref="ReloadBootstrap"/>); the build it replaces carried what the world cannot tell while it quiesced. With hot
    /// reload on, every generation also logs its reload digests (<see cref="ReloadDigests"/>) for
    /// <c>scripts/run-hot-reload-check.sh</c>; with it off none of this runs.
    /// </summary>
    public sealed class GameGeneration : IGameplayGeneration
    {
        private const string BootPhase = "boot";
        private const string QuiescePhase = "quiesce";
        private const string ReloadPhase = "reload";

        private static readonly Lazy<int> SchemaVersion = new(() => NewestServerMigration(typeof(GameGeneration).Assembly.GetTypes()));

        private readonly IGenerationBoot _boot;
        private readonly Action<string> _log;
        private string _sourceDirectory;
        private IHostCallbacks _hostCallbacks;
        private HostedSignalHandler _signals;
        private INativeSinkBinding _sinkBinding;
        private NativeSinkSet _sinks;
        private bool _bootStarted;
        private long _generation;
        private bool _reloadEnabled;

        /// <summary>The generation the host loads: the server's own boot.</summary>
        public GameGeneration()
            : this(new HostedServerBoot())
        {
        }

        public GameGeneration(IGenerationBoot boot)
            : this(boot, Console.WriteLine)
        {
        }

        /// <summary>For tests: <paramref name="log"/> stands in for the server console.</summary>
        public GameGeneration(IGenerationBoot boot, Action<string> log)
        {
            _boot = boot ?? throw new ArgumentNullException(nameof(boot));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        /// <summary>This build's identity: the game assembly's module version id.</summary>
        public string BuildId => typeof(GameGeneration).Assembly.ManifestModule.ModuleVersionId.ToString("N");

        /// <summary>The run-script dispatcher, once activated: the server's script executor.</summary>
        public IScriptDispatcher Scripts { get; private set; }

        /// <summary>The main-loop sink, once activated: the server's main loop for the frame the host delivers.</summary>
        public IMainLoopSink MainLoop { get; private set; }

        /// <summary>
        /// The newest server migration this build carries (<see cref="IServerMigration.Version"/>), or
        /// <see cref="MigrationService.RetiredServerMigrationVersion"/> when it carries none: the host's schema gate
        /// (<c>SWLOR.Game.Host.Reload.ReloadSchemaGate</c>) rejects a reload candidate whose version is newer than the database's
        /// <c>MigrationVersion</c>. The host does not gate the first activation.
        /// </summary>
        public int RequiredSchemaVersion => SchemaVersion.Value;

        /// <summary>The directory the host loaded this build from; the engine-test assembly is looked for there.</summary>
        public void ConfigureSourceDirectory(string directoryPath)
        {
            _sourceDirectory = directoryPath;
        }

        /// <summary>
        /// Builds the service container with validation and throws it away, off the engine thread, so a build whose
        /// registrations or constructors fail is rejected before the engine is touched.
        /// </summary>
        public ValueTask ValidateAsync(CancellationToken cancellationToken)
        {
            return new ValueTask(Task.Run(_boot.Validate, cancellationToken));
        }

        /// <summary>
        /// On the engine thread: binds this build's sinks to the host's native hooks, then boots the server with what the
        /// host hands it (its closure handler, its Redis connection, its hook control, the build's directory), which
        /// registers the loggers, sets the script execution provider, builds the container and registers the script
        /// handlers, then exposes the script dispatcher and the main loop and binds the signal handler. The hooks stay
        /// uninstalled until the game's boot handlers enable them, at the same moments as on the native entry.
        /// When the host says the module has already loaded (<see cref="IHostLifecycle.ModuleLoaded"/>, a reload), the
        /// server also takes the module-load signal the engine will not send again and runs the reload bootstrap (the rerun
        /// boot handlers, then the adoption steps), and the activation is logged with how long it took and its slowest steps:
        /// it is the new build's part of the reload pause. With hot reload on, the adopted-state and world digests are logged
        /// before the pause ends and the cache digests at the next frame.
        /// </summary>
        public ValueTask ActivateAsync(GenerationActivation activation, CancellationToken cancellationToken)
        {
            if (activation == null)
                throw new ArgumentNullException(nameof(activation));

            var services = activation.HostServices ?? throw new InvalidOperationException("The host hands the game no services.");
            var closures = Require<global::NWN.Core.ICoreFunctionHandler>(services);
            var redisConnection = Require<IRedisConnectionSource>(services);
            var hostCallbacks = Require<IHostCallbacks>(services);
            var hookControl = Require<IHookControl>(services);
            var sinkBinding = Require<INativeSinkBinding>(services);
            // A host that hands no lifecycle never reloads: every activation is a boot.
            var lifecycle = services.Resolve(typeof(IHostLifecycle)) as IHostLifecycle;
            var reload = lifecycle is { ModuleLoaded: true };
            var activationTime = reload ? Stopwatch.StartNew() : null;
            _generation = activation.Generation;
            _reloadEnabled = lifecycle is { ReloadEnabled: true };

            _sinks = NativeSinkFactory.Create();
            _sinkBinding = sinkBinding;
            sinkBinding.Bind(_sinks);

            _bootStarted = true;
            _boot.Activate(new HostedBoot(closures, redisConnection, hookControl, _sourceDirectory));
            var scripts = new HostedScriptDispatcher();
            IReadOnlyList<ReloadStepResult> rebuilt = null;
            IReadOnlyList<ReloadStepResult> restored = null;
            if (reload)
            {
                // The package makes this generation active only once the activation returns; until then the scripts the engine
                // runs from the reload bootstrap (the skill cache's loaded event, say) come here, as they reach the build at boot.
                hostCallbacks.BindActivationScripts(scripts);
                try
                {
                    _boot.ResumeAfterModuleLoad();
                    rebuilt = _boot.RebuildAfterReload();

                    // The adopted and carried state is compared with the old build's as the adoption left it, before the players'
                    // restore steps use it.
                    if (_reloadEnabled)
                        LogDigests(ReloadPhase, _boot.DescribeAdoptedState);

                    restored = RestorePlayers(activation.Generation);
                }
                finally
                {
                    hostCallbacks.UnbindActivationScripts(scripts);
                }
            }

            // With hot reload on, the boot logs its cache digests at the frame after the module's load script has run (every boot
            // script has run by then; NWNX signals the module load before them).
            Scripts = !reload && _reloadEnabled
                ? new ModuleLoadedScriptDispatcher(scripts, () => _boot.RunNextFrame(() => LogDigests(BootPhase, _boot.DescribeCaches)))
                : scripts;
            MainLoop = new HostedMainLoop(hostCallbacks);
            _signals = new HostedSignalHandler(_boot.ProcessSignal);
            hostCallbacks.BindSignals(_signals);
            _hostCallbacks = hostCallbacks;

            if (activationTime != null)
            {
                _log(FormattableString.Invariant(
                    $"Generation {activation.Generation} activated by a reload in {activationTime.Elapsed.TotalMilliseconds:F0} ms: ") +
                    "rebuilt the service container and the script dispatch, then the reload bootstrap " +
                    ReloadBootstrap.Describe(rebuilt ?? Array.Empty<ReloadStepResult>()) + "; the online players " +
                    ReloadBootstrap.Describe(restored ?? Array.Empty<ReloadStepResult>()));

                if (_reloadEnabled)
                    _boot.RunNextFrame(() => LogDigests(ReloadPhase, _boot.DescribeCaches));
            }

            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// On the engine thread, when a reload replaces this build and hot reload is on: logs the adopted-state and world digests
        /// the next build's must equal, then runs the carry steps, which leave on the module what the next build's adoption
        /// steps cannot read back from the world. The old build's dispose then stops its timers and queues
        /// (<see cref="IServerTeardown"/>), and the host drops its pending closures. P4-15 saves online players' state and closes
        /// windows and conversations here.
        /// </summary>
        public ValueTask BeginQuiescenceAsync(CancellationToken cancellationToken)
        {
            if (!_reloadEnabled || !_bootStarted)
                return ValueTask.CompletedTask;

            try
            {
                var players = _boot.QuiescePlayers();
                _log(FormattableString.Invariant($"Generation {_generation} quiesced its online players for a reload: ") + ReloadBootstrap.Describe(players));
            }
            catch (Exception exception)
            {
                // The players keep what the engine and Redis hold; the next build's restore steps still run.
                _log(FormattableString.Invariant($"Generation {_generation} could not quiesce its online players for the reload: ") + exception);
            }

            LogDigests(QuiescePhase, _boot.DescribeAdoptedState);
            try
            {
                var carried = _boot.CarryForReload();
                _log(FormattableString.Invariant($"Generation {_generation} quiesced for a reload: ") + ReloadBootstrap.Describe(carried));
            }
            catch (Exception exception)
            {
                // The next build's adoption steps find nothing carried and say so; the reload itself goes on.
                _log(FormattableString.Invariant($"Generation {_generation} could not carry its state for the reload: ") + exception);
            }

            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// The online players' restore steps and screens (<see cref="IGenerationBoot.RestorePlayers"/>). A failure is logged and the
        /// activation goes on: the players keep what the engine and Redis hold.
        /// </summary>
        private IReadOnlyList<ReloadStepResult> RestorePlayers(long generation)
        {
            try
            {
                return _boot.RestorePlayers();
            }
            catch (Exception exception)
            {
                _log(FormattableString.Invariant($"Generation {generation} could not restore its online players after the reload: ") + exception);
                return Array.Empty<ReloadStepResult>();
            }
        }

        /// <summary>Nothing yet: the server drains no work of its own before its dispose.</summary>
        public ValueTask DrainAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// On the engine thread: unbinds this build's sinks, so every native hook runs the engine function again and none
        /// references a type of this build, stops taking signals, scripts and frames, then lets go of the server
        /// (<see cref="IServerTeardown"/>) and gives NWN.Core back to the host.
        /// </summary>
        public ValueTask DisposeAsync()
        {
            if (_sinkBinding != null && _sinks != null)
                _sinkBinding.Unbind(_sinks);
            _sinkBinding = null;
            _sinks = null;

            if (_hostCallbacks != null && _signals != null)
                _hostCallbacks.UnbindSignals(_signals);

            Scripts = null;
            MainLoop = null;
            _signals = null;
            _hostCallbacks = null;

            // Also after an activation that failed part way: whatever the boot set up is let go of, and NWN.Core is the host's again.
            if (_bootStarted)
            {
                _bootStarted = false;
                _boot.Retire();
            }

            return ValueTask.CompletedTask;
        }

        /// <summary>The newest server migration among <paramref name="types"/>, or the retired version when there is none.</summary>
        public static int NewestServerMigration(Type[] types)
        {
            var versions = types
                .Where(type => typeof(IServerMigration).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                .Select(type => ((IServerMigration)Activator.CreateInstance(type)).Version)
                .ToList();

            return versions.Count == 0
                ? MigrationService.RetiredServerMigrationVersion
                : Math.Max(versions.Max(), MigrationService.RetiredServerMigrationVersion);
        }

        /// <summary>
        /// Logs every digest <paramref name="describe"/> returns for this generation and <paramref name="phase"/>, then how long
        /// it took. A digest that fails is logged and skipped: it is a check, never part of what the server does.
        /// </summary>
        private void LogDigests(string phase, Func<IReadOnlyList<ReloadDigestLine>> describe)
        {
            var time = Stopwatch.StartNew();
            try
            {
                var lines = describe();
                foreach (var line in lines)
                    _log(line.Format(_generation, phase));

                _log(FormattableString.Invariant(
                    $"{ReloadDigestLine.LogPrefix}S generation={_generation} phase={phase} lines={lines.Count} ms={time.Elapsed.TotalMilliseconds:F0}"));
            }
            catch (Exception exception)
            {
                _log(FormattableString.Invariant($"{ReloadDigestLine.LogPrefix}S generation={_generation} phase={phase} failed: ") + exception);
            }
        }

        private static TService Require<TService>(IHostServices services)
            where TService : class
        {
            return services.Resolve(typeof(TService)) as TService
                   ?? throw new InvalidOperationException($"The host hands the game no {typeof(TService).Name}.");
        }
    }
}
