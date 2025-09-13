using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using System;

namespace SWLOR.Game.Server.Core
{
    public class ServerBootstrapper
    {
        private readonly ClosureManager _closureManager;

        public ServerBootstrapper()
        {
            _closureManager = new ClosureManager();
        }

        public ClosureManager ClosureManager => _closureManager;

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
            ServerManager.NativeInterop.RegisterHandlers();
        }

        private void InitializeSWLORSystems()
        {
            Console.WriteLine("Initializing SWLOR internal systems...");
            Environment.SetEnvironmentVariable("GAME_SERVER_CONTEXT", "true");

            Console.WriteLine("Registering loggers...");
            Log.Register();
            Console.WriteLine("Loggers registered successfully.");
        }

        private void RegisterEventHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        private void LoadScripts()
        {
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