using SWLOR.Core.Async;
using SWLOR.Core.Extension;
using SWLOR.Core.LogService;
using SWLOR.Core.Plugin;

namespace SWLOR.Core
{
    public static class Internal
    {

        private const int ScriptHandled = 0;
        private const int ScriptNotHandled = -1;

        private static readonly EventManager _eventManager = new();

        public static event Action OnScriptContextBegin;
        public static event Action OnScriptContextEnd;

        private static ICoreEventHandler _coreGameManager;

        public static int Bootstrap(IntPtr nativeHandlesPtr, int nativeHandlesLength)
        {
            Environment.SetEnvironmentVariable("GAME_SERVER_CONTEXT", "true");

            Console.WriteLine("Registering plugins...");
            PluginManager.Load();
            Console.WriteLine($"Plugins registered successfully.");
            
            var retVal = NWNCore.Init(nativeHandlesPtr, nativeHandlesLength, out CoreGameManager coreGameManager);
            coreGameManager.OnSignal += OnSignal;
            coreGameManager.OnServerLoop += OnServerLoop;
            coreGameManager.OnRunScript += OnRunScript;
            _coreGameManager = coreGameManager;

            Console.WriteLine("Registering loggers...");
            Log.Register();
            Console.WriteLine("Loggers registered successfully.");

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            Console.WriteLine("Registering scripts...");
            _eventManager.LoadHandlersFromAssembly();
            Console.WriteLine("Scripts registered successfully.");

            return retVal;
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs ex)
        {
            Log.Write(LogGroup.Error, ((Exception)ex.ExceptionObject).ToMessageAndCompleteStacktrace());
        }

        /// <summary>
        /// Directly executes a script. This bypasses the NWScript round-trip.
        /// </summary>
        /// <param name="scriptName">Name of the script.</param>
        /// <param name="objectSelf">The object to execute the script upon.</param>
        public static void DirectRunScript(string scriptName, uint objectSelf)
        {
            _coreGameManager.OnRunScript(scriptName, objectSelf);
        }

        private static void OnRunScript(string scriptName, uint objectSelf, out int scriptHandlerResult)
        {
            var retVal = RunScripts(scriptName);

            scriptHandlerResult = retVal == -1 ? ScriptNotHandled : retVal;
        }

        private static void OnSignal(string signal)
        {

        }

        private static void OnServerLoop(ulong frame)
        {
            OnScriptContextBegin?.Invoke();

            try
            {
                NwTask.MainThreadSynchronizationContext.Update();
                Scheduler.Process();
            }
            catch (Exception ex)
            {
                Log.Write(LogGroup.Error, ex.ToMessageAndCompleteStacktrace());
            }

            OnScriptContextEnd?.Invoke();
        }


        private static int RunScripts(string script)
        {
            if (_eventManager.ConditionalScriptExists(script))
            {
                return _eventManager.RunConditionalScript(script) ? 1 : 0;
            }
            else if (_eventManager.ScriptExists(script))
            {
                _eventManager.RunScript(script);
                return ScriptHandled;
            }

            return ScriptNotHandled;
        }
    }
}
