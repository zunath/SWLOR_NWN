using System.Collections.Generic;
using System.IO;
using System.Runtime.Loader;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API;

namespace SWLOR.Game.Server.Core
{
    public class ServerBootstrapper
    {
        private readonly ClosureManager _closureManager;
        private readonly ContainerHost _container;

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
            Steps = CreateSteps();
        }

        public ClosureManager ClosureManager => _closureManager;

        /// <summary>
        /// The boot steps in the order <see cref="Bootstrap"/> runs them. One list drives the run and the tests that
        /// pin the order, so the order they check is the order that runs.
        /// </summary>
        public IReadOnlyList<BootstrapStep> Steps { get; }

        public void Bootstrap()
        {
            Run(Steps);
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
        private BootstrapStep[] CreateSteps()
        {
            return new[]
            {
                new BootstrapStep(nameof(InitializeNWNCore), InitializeNWNCore),
                new BootstrapStep(nameof(RegisterNativeHandlers), RegisterNativeHandlers),
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
            var registrations = CompositionRoot.Build(_container);
            Console.WriteLine($"Service container built ({registrations} registrations).");
        }

        private void RegisterEventHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
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
                var candidateDirectories = new[]
                {
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
            ServerManager.Scripts.LoadHandlersFromAssembly();
            Console.WriteLine("Scripts registered successfully.");
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs ex)
        {
            Log.Write(LogGroup.Error, ((Exception)ex.ExceptionObject).ToMessageAndCompleteStacktrace(), true);
        }
    }
}
