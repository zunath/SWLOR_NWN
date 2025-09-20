using System;
using NWN.Core;
using SWLOR.NWN.API;
using SWLOR.Shared.Core.Extension;
using SWLOR.Shared.Core.Log;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Core.Server.Contracts;

namespace SWLOR.Shared.Core.Server
{
    public class ServerBootstrapper : IServerBootstrapper
    {
        private readonly ILogger _logger;
        private readonly ICoreFunctionHandler _closureManager;
        private readonly INativeInteropManager _nativeInterop;
        private readonly IScriptRegistry _scriptRegistry;
        private readonly IScriptExecutionProvider _executionProvider;

        public ServerBootstrapper(
            ILogger logger,
            INativeInteropManager nativeInterop,
            ICoreFunctionHandler closureManager,
            IScriptRegistry scriptRegistry,
            IScriptExecutionProvider executionProvider)
        {
            _logger = logger;
            _nativeInterop = nativeInterop;
            _closureManager = closureManager;
            _scriptRegistry = scriptRegistry;
            _executionProvider = executionProvider;
        }

        public void Bootstrap()
        {
            try
            {
                Console.WriteLine("SWLOR Server starting with new bootstrap method...");

                InitializeNWNCore();
                RegisterNativeHandlers();
                InitializeSWLORSystems();
                RegisterEventHandlers();
                LoadScripts();

                Console.WriteLine("SWLOR Server bootstrap complete.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Bootstrap failed: {e}");
                throw;
            }
        }

        private void InitializeNWNCore()
        {
            global::NWN.Core.NWNCore.Init(_closureManager);
            Console.WriteLine("NWN.Core library initialized successfully.");
        }

        private void RegisterNativeHandlers()
        {
            _nativeInterop.RegisterHandlers();
        }

        private void InitializeSWLORSystems()
        {
            Console.WriteLine("Initializing SWLOR internal systems...");
            Environment.SetEnvironmentVariable("GAME_SERVER_CONTEXT", "true");

            Console.WriteLine("Registering script execution provider...");
            NWN.API.ScriptExecutionProvider.SetProvider(_executionProvider);
            Console.WriteLine("Script execution provider registered successfully.");
        }

        private void RegisterEventHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        private void LoadScripts()
        {
            Console.WriteLine("Registering scripts...");
            _scriptRegistry.LoadHandlersFromAssembly();
            Console.WriteLine("Scripts registered successfully.");
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs ex)
        {
            _logger.Write<ErrorLogGroup>(((Exception)ex.ExceptionObject).ToMessageAndCompleteStacktrace(), true);
        }
    }
}