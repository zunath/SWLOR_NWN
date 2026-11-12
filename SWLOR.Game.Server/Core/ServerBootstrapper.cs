using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API;

namespace SWLOR.Game.Server.Core
{
    public class ServerBootstrapper
    {
        private readonly ContainerHost _container;
        private ClosureManager _closureManager;

        // Set only on the host entry (BootstrapHosted); null on the native entry, whose steps then run exactly as before.
        private HostedBoot _hostedBoot;

        public ServerBootstrapper()
            : this(CompositionRoot.ServerHost)
        {
        }

        /// <param name="container">
        /// The container the <c>BuildServiceContainer</c> step builds. The server always uses
        /// <see cref="CompositionRoot.ServerHost"/>; tests pass their own.
        /// </param>
        public ServerBootstrapper(ContainerHost container)
        {
            _closureManager = new ClosureManager();
            _container = container ?? throw new ArgumentNullException(nameof(container));
            var shared = CreateSharedSteps();
            Steps = CreateSteps(shared);
            HostedSteps = CreateHostedSteps(shared);
        }

        /// <summary>
        /// The OBJECT_SELF owner and NWN.Core's closure handler: a <see cref="ClosureManager"/> on the native entry, a
        /// <see cref="HostedClosureManager"/> once the host entry's first step has run.
        /// </summary>
        public ClosureManager ClosureManager => _closureManager;

        /// <summary>The removable process-event subscriptions; available once the container is built.</summary>
        public IProcessEventSubscriptions ProcessEvents => _container.Resolve<IProcessEventSubscriptions>();

        /// <summary>
        /// The boot steps in the order <see cref="Bootstrap"/> runs them. One list drives the run and the tests that
        /// pin the order, so the order they check is the order that runs.
        /// </summary>
        public IReadOnlyList<BootstrapStep> Steps { get; }

        /// <summary>
        /// The boot steps of the host entry (<c>SWLOR.Game.Host</c>), in the order <see cref="BootstrapHosted"/> runs them.
        /// The host has already initialized NWN.Core and registered the native callbacks, so the two native steps of
        /// <see cref="Steps"/> become <c>InitializeHostedNWNCore</c>; the other five are the same step objects.
        /// </summary>
        public IReadOnlyList<BootstrapStep> HostedSteps { get; }

        public void Bootstrap()
        {
            Run(Steps);
        }

        /// <summary>Boots the game build the host loaded as a generation: <see cref="HostedSteps"/>, with what the host hands it.</summary>
        public void BootstrapHosted(HostedBoot boot)
        {
            _hostedBoot = boot ?? throw new ArgumentNullException(nameof(boot));
            Run(HostedSteps);
        }

        /// <summary>
        /// Lets go of the hosted game build: the server teardown (<see cref="IServerTeardown"/>: timers, queues, the
        /// <c>app_shutdown</c> clean-up, the process-wide subscriptions, the script execution provider), then NWN.Core goes
        /// back to the host's closure handler, so nothing of this build stays reachable from it.
        /// </summary>
        public void ShutdownHosted()
        {
            var boot = _hostedBoot ?? throw new InvalidOperationException("The server was not booted by the host.");
            try
            {
                if (_container.IsBuilt)
                    _container.Resolve<IServerTeardown>().Run();
                else
                    ReleasePartialBoot(new ServerStaticState());
            }
            finally
            {
                global::NWN.Core.NWNCore.Init(boot.HostClosures);
            }
        }

        /// <summary>
        /// The clean-up of a boot that stopped before the container was built: the steps before it only registered the
        /// loggers and set the script execution provider (<c>InitializeSWLORSystems</c>), so those are let go of.
        /// </summary>
        public static void ReleasePartialBoot(IServerStaticState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            try
            {
                state.FlushLogs();
            }
            finally
            {
                state.ClearScriptExecutionProvider();
            }
        }

        /// <summary>Runs the steps in order. A step that throws stops the boot; the exception is logged and rethrown.</summary>
        public static void Run(IEnumerable<BootstrapStep> steps)
        {
            try
            {
                Console.WriteLine("SWLOR Server starting with new bootstrap method...");

                foreach (var step in steps)
                    step.Run();

                Console.WriteLine("SWLOR Server bootstrap complete.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Bootstrap failed: {e}");
                throw;
            }
        }

        /// <summary>
        /// The order matters. The service container is built after the internal systems (logging, the script execution
        /// provider) are up and before anything that can run a handler: handlers only exist once <see cref="LoadScripts"/>
        /// has registered them, and the engine cannot call back into the server before this method returns. A converted
        /// service's static facade resolves its instance from the container, so every handler, including those of
        /// <c>mod_preload</c>, finds it built.
        /// </summary>
        private BootstrapStep[] CreateSteps(BootstrapStep[] shared)
        {
            var native = new[]
            {
                new BootstrapStep(nameof(InitializeNWNCore), InitializeNWNCore),
                new BootstrapStep(nameof(RegisterNativeHandlers), RegisterNativeHandlers)
            };
            return native.Concat(shared).ToArray();
        }

        private BootstrapStep[] CreateHostedSteps(BootstrapStep[] shared)
        {
            var hosted = new[]
            {
                new BootstrapStep(nameof(InitializeHostedNWNCore), InitializeHostedNWNCore)
            };
            return hosted.Concat(shared).ToArray();
        }

        /// <summary>The steps both entries run, in this order, after their own NWN.Core step(s).</summary>
        private BootstrapStep[] CreateSharedSteps()
        {
            return new[]
            {
                new BootstrapStep(nameof(InitializeSWLORSystems), InitializeSWLORSystems),
                new BootstrapStep(nameof(BuildServiceContainer), BuildServiceContainer),
                new BootstrapStep(nameof(RegisterEventHandlers), RegisterEventHandlers),
                new BootstrapStep(nameof(LoadEngineTestAssembly), LoadEngineTestAssembly),
                new BootstrapStep(nameof(LoadScripts), LoadScripts)
            };
        }

        private void InitializeNWNCore()
        {
            global::NWN.Core.NWNCore.Init(_closureManager);
            Console.WriteLine("NWN.Core library initialized successfully.");
        }

        /// <summary>
        /// The host entry's counterpart of <see cref="InitializeNWNCore"/>. The host initialized NWN.Core with its own
        /// closure handler; the game puts its <see cref="HostedClosureManager"/> in front of it, so OBJECT_SELF stays the
        /// game's, as on the native entry, while the host schedules the closures and tracks them for this generation.
        /// </summary>
        private void InitializeHostedNWNCore()
        {
            var boot = _hostedBoot ?? throw new InvalidOperationException("The host entry's steps run only through BootstrapHosted.");
            _closureManager = new HostedClosureManager(boot.HostClosures);
            global::NWN.Core.NWNCore.Init(_closureManager);
            Console.WriteLine("NWN.Core library initialized successfully (hosted).");
        }

        private void RegisterNativeHandlers()
        {
            ServerManager.NativeInterop.RegisterHandlers();
        }

        private void InitializeSWLORSystems()
        {
            Console.WriteLine("Initializing SWLOR internal systems...");
            Environment.SetEnvironmentVariable("GAME_SERVER_CONTEXT", "true");

            Console.WriteLine("Registering loggers...");
            Log.Register();
            Console.WriteLine("Loggers registered successfully.");

            Console.WriteLine("Registering script execution provider...");
            ScriptExecutionProvider.SetProvider(new ScriptExecutionProviderImpl());
            Console.WriteLine("Script execution provider registered successfully.");
        }

        private void BuildServiceContainer()
        {
            Console.WriteLine("Building the service container...");
            // The host entry hands the game the host's Redis connection, which outlives the build; the native entry keeps
            // the server's own (RedisConnectionSource).
            var registrations = _hostedBoot == null
                ? CompositionRoot.Build(_container)
                : CompositionRoot.Build(_container, _hostedBoot.RedisConnection);
            Console.WriteLine($"Service container built ({registrations} registrations).");
        }

        private void RegisterEventHandlers()
        {
            _container.Resolve<IProcessEventSubscriptions>().SubscribeUnhandledException(OnUnhandledException);
        }

        /// <summary>
        /// Loads the optional in-engine test assembly (SWLOR.Game.Server.EngineTests) when
        /// engine tests are enabled. That assembly lives outside the game project so test code
        /// never mixes with game code and never ships: a production deploy stages only the game
        /// project's output, so the DLL is simply absent there and this is a no-op.
        ///
        /// Must run BEFORE LoadScripts - the script registry discovers [NWNEventHandler] methods
        /// by scanning loaded assemblies, and since no game code references this one it is never
        /// loaded implicitly. It is loaded into the GAME assembly's own load context: a plain
        /// Assembly.LoadFrom would land in the default context and hand the test code a second,
        /// incompatible copy of every game type it references.
        /// </summary>
        private void LoadEngineTestAssembly()
        {
            if (!ApplicationSettings.Get().EngineTestsEnabled)
                return;

            const string EngineTestAssemblyName = "SWLOR.Game.Server.EngineTests";
            try
            {
                var gameAssembly = typeof(ServerBootstrapper).Assembly;

                // The test assembly is deployed BESIDE the game assembly. Resolve that
                // directory from the game assembly's own location rather than
                // AppContext.BaseDirectory: under the NWNX .NET host those are not the same
                // directory, and using BaseDirectory silently finds nothing (the server then
                // idles forever with no tests scheduled). BaseDirectory is kept only as a
                // fallback for hosts where Location is empty.
                // The host loads the game build from bytes, so the assembly has no location; the host names the
                // directory it loaded the build from instead, and it is probed first.
                var candidateDirectories = _hostedBoot == null
                    ? new[]
                    {
                        Path.GetDirectoryName(gameAssembly.Location),
                        AppContext.BaseDirectory
                    }
                    : new[]
                    {
                        _hostedBoot.GameDirectory,
                        Path.GetDirectoryName(gameAssembly.Location),
                        AppContext.BaseDirectory
                    };

                var probed = new List<string>();
                foreach (var directory in candidateDirectories)
                {
                    if (string.IsNullOrWhiteSpace(directory))
                        continue;

                    var path = Path.Combine(directory, $"{EngineTestAssemblyName}.dll");
                    probed.Add(path);
                    if (!File.Exists(path))
                        continue;

                    var loadContext = AssemblyLoadContext.GetLoadContext(gameAssembly) ?? AssemblyLoadContext.Default;
                    loadContext.LoadFromAssemblyPath(path);
                    Console.WriteLine($"{EngineTestAssemblyName} loaded from {path} - engine tests will be discovered.");
                    return;
                }

                // Loud and specific: engine tests were explicitly requested, so a missing
                // harness is a misconfiguration, and naming every path tried makes it a
                // one-line diagnosis instead of a mystery idle server.
                Console.WriteLine(
                    $"ENGINE TEST HARNESS MISSING - engine tests are enabled but {EngineTestAssemblyName}.dll was not found. Probed: {string.Join(", ", probed)}");
            }
            catch (Exception ex)
            {
                // A broken test harness must never take the server down with it.
                Console.WriteLine($"Failed to load {EngineTestAssemblyName}: {ex}");
            }
        }

        private void LoadScripts()
        {
            // Every handler forwards to a service from the container, so none may be registered without it.
            if (!_container.IsBuilt)
                throw new ContainerNotBuiltException("The service container must be built before the script handlers are registered.");

            Console.WriteLine("Registering scripts...");
            // A handler declared on a service's instance method runs on the container's singleton of that class.
            ServerManager.Scripts.LoadHandlersFromAssembly(_container.ResolveHandlerOwner);
            Console.WriteLine("Scripts registered successfully.");
        }

        private static void OnUnhandledException(object exceptionObject)
        {
            Log.Write(LogGroup.Error, ((Exception)exceptionObject).ToMessageAndCompleteStacktrace(), true);
        }
    }
}
