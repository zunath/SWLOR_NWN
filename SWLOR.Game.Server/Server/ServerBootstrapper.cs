using System;
using NWN.Core;
using SWLOR.NWN.API.Contracts;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Extensions;
using SWLOR.Shared.Core.Extension;
using SWLOR.Shared.Core.Log.LogGroup;

namespace SWLOR.Game.Server.Server
{
    public class ServerBootstrapper : IServerBootstrapper
    {
        private readonly ILogger _logger;
        private readonly IDatabaseService _databaseService;
        private readonly ICoreFunctionHandler _coreFunctionHandler;
        private readonly INativeInteropManager _nativeInterop;
        private readonly IScriptRegistry _scriptRegistry;
        private readonly IScriptExecutionProvider _executionProvider;

        public ServerBootstrapper(
            ILogger logger,
            IDatabaseService databaseService,
            INativeInteropManager nativeInterop,
            ICoreFunctionHandler coreFunctionHandler,
            IScriptRegistry scriptRegistry,
            IScriptExecutionProvider executionProvider)
        {
            _logger = logger;
            _databaseService = databaseService;
            _nativeInterop = nativeInterop;
            _coreFunctionHandler = coreFunctionHandler;
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
                _databaseService.Load();

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
            NWNCore.Init(_coreFunctionHandler);
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
            NWN.API.Service.ScriptExecutionProvider.SetProvider(_executionProvider);
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
