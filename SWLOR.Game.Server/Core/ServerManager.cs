using SWLOR.Game.Server.Core.Async;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.Core;
using SWLOR.NWN.API.NWNX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace SWLOR.Game.Server.Core
{
    public static unsafe class ServerManager
    {
        private class ActionScript
        {
            public Action Action { get; set; }
            public string Name { get; set; }
        }

        private class ConditionalScript
        {
            public ConditionalScriptDelegate Action { get; set; }
            public string Name { get; set; }
        }

        private static ClosureManager _closureManager;
        private static readonly Encoding _encoding;

        private const int MaxCharsInScriptName = 16;
        private const int ScriptHandled = 0;
        private const int ScriptNotHandled = -1;

        private delegate bool ConditionalScriptDelegate();
        public static event Action OnScriptContextBegin;
        public static event Action OnScriptContextEnd;

        private static Dictionary<string, List<ActionScript>> _scripts;
        private static Dictionary<string, List<ConditionalScript>> _conditionalScripts;

        static ServerManager()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            _encoding = Encoding.GetEncoding("windows-1252");
        }

        /// <summary>
        /// New bootstrap method called by NWNX DotNET plugin
        /// </summary>
        public static void Bootstrap()
        {
            try
            {
                Console.WriteLine("SWLOR Server starting with new bootstrap method...");

                _closureManager = new ClosureManager();

                // Initialize NWN.Core library
                global::NWN.Core.NWNCore.Init(_closureManager);
                Console.WriteLine("NWN.Core library initialized successfully.");

                // Register all handlers with the new NWNX API
                NWNXPInvoke.RegisterMainLoopHandler(&OnMainLoop);
                NWNXPInvoke.RegisterRunScriptHandler(&OnRunScript);
                NWNXPInvoke.RegisterClosureHandler(&OnClosure);
                NWNXPInvoke.RegisterSignalHandler(&OnSignal);

                // Initialize the SWLOR internal systems
                Console.WriteLine("Initializing SWLOR internal systems...");
                Environment.SetEnvironmentVariable("GAME_SERVER_CONTEXT", "true");

                Console.WriteLine("Registering loggers...");
                Log.Register();
                Console.WriteLine("Loggers registered successfully.");

                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

                Console.WriteLine("Registering scripts...");
                LoadHandlersFromAssembly();
                Console.WriteLine("Scripts registered successfully.");
                Console.WriteLine("SWLOR Server bootstrap complete.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Bootstrap failed: {e}");
                throw;
            }
        }

        [UnmanagedCallersOnly]
        private static void OnMainLoop(ulong frame)
        {
            try
            {
                ProcessMainLoop(frame);
            }
            catch (Exception e)
            {
                Log.Write(LogGroup.Error, $"MainLoop exception: {e}");
            }
        }

        [UnmanagedCallersOnly]
        private static int OnRunScript(byte* pScript, uint oidSelf)
        {
            try
            {
                string scriptName = ReadNullTerminatedString(pScript);
                return ProcessRunScript(scriptName, oidSelf);
            }
            catch (Exception e)
            {
                Log.Write(LogGroup.Error, $"RunScript exception: {e}");
                return -1; // Script not handled
            }
        }

        [UnmanagedCallersOnly]
        private static void OnSignal(byte* pSignal)
        {
            try
            {
                string signal = ReadNullTerminatedString(pSignal);
                ProcessSignal(signal);
            }
            catch (Exception e)
            {
                Log.Write(LogGroup.Error, $"Signal processing exception: {e}");
            }
        }

        [UnmanagedCallersOnly]
        private static void OnClosure(ulong eid, uint oidSelf)
        {
            try
            {
                _closureManager.OnClosure(eid, oidSelf);
            }
            catch (Exception e)
            {
                Log.Write(LogGroup.Error, $"Closure processing exception: {e}");
            }
        }

        private static string ReadNullTerminatedString(byte* ptr)
        {
            if (ptr == null)
                return string.Empty;

            int length = 0;
            while (ptr[length] != 0)
                length++;

            if (length == 0)
                return string.Empty;

            var bytes = new byte[length];
            for (int i = 0; i < length; i++)
                bytes[i] = ptr[i];

            return _encoding.GetString(bytes);
        }

        /// <summary>
        /// Script execution processing called by the new bootstrap
        /// </summary>
        private static int ProcessRunScript(string scriptName, uint objectSelf)
        {
            // Set the script execution context
            var oldObjectSelf = _closureManager?.ObjectSelf ?? 0x7F000000; // OBJECT_INVALID
            if (_closureManager is ClosureManager manager)
                manager.ObjectSelf = objectSelf;

            try
            {
                var retVal = RunScripts(scriptName);
                return retVal == -1 ? ScriptNotHandled : retVal;
            }
            finally
            {
                // Restore the previous script context
                if (_closureManager is ClosureManager coreManager)
                    coreManager.ObjectSelf = oldObjectSelf;
            }
        }


        private static int RunScripts(string script)
        {
            if (_conditionalScripts.ContainsKey(script))
            {
                // Always default conditional scripts to true. If one or more of the actions return a false,
                // we will return a false (even if others are true).
                // This ensures all actions get fired when the script is called.
                var result = true;

                if (_conditionalScripts.ContainsKey(script))
                {
                    foreach (var action in _conditionalScripts[script])
                    {
                        ProfilerPlugin.PushPerfScope(OBJECT_SELF, action.Name);
                        var actionResult = action.Action.Invoke();
                        ProfilerPlugin.PopPerfScope();

                        if (result)
                            result = actionResult;
                    }
                }

                return result ? 1 : 0;
            }
            else if (_scripts.ContainsKey(script))
            {
                if (_scripts.ContainsKey(script))
                {
                    foreach (var action in _scripts[script])
                    {
                        try
                        {
                            ProfilerPlugin.PushPerfScope(OBJECT_SELF, action.Name);
                            action.Action();
                            ProfilerPlugin.PopPerfScope();
                        }
                        catch (Exception ex)
                        {
                            Log.Write(LogGroup.Error, $"C# Script '{script}' threw an exception. Details: {Environment.NewLine}{Environment.NewLine}{ex.ToMessageAndCompleteStacktrace()}");
                        }
                    }
                }

                return ScriptHandled;
            }

            return ScriptNotHandled;
        }


        private static void LoadHandlersFromAssembly()
        {
            _scripts = new Dictionary<string, List<ActionScript>>();
            _conditionalScripts = new Dictionary<string, List<ConditionalScript>>();

            var handlers = Assembly.GetExecutingAssembly()
                .GetTypes()
                .SelectMany(t => t.GetMethods())
                .Where(m => m.GetCustomAttributes(typeof(NWNEventHandler), false).Length > 0)
                .ToArray();

            foreach (var mi in handlers)
            {
                foreach (var attr in mi.GetCustomAttributes(typeof(NWNEventHandler), false))
                {
                    var script = ((NWNEventHandler)attr).Script;
                    if (script.Length > MaxCharsInScriptName || script.Length == 0)
                    {
                        Log.Write(LogGroup.Error, $"Script name '{script}' is invalid on method {mi.Name}.");
                        throw new ApplicationException();
                    }

                    // If the return type is a bool, it is assumed to be a conditional script.
                    if (mi.ReturnType == typeof(bool))
                    {
                        var del = (ConditionalScriptDelegate)mi.CreateDelegate(typeof(ConditionalScriptDelegate));

                        if (!_conditionalScripts.ContainsKey(script))
                            _conditionalScripts[script] = new List<ConditionalScript>();

                        _conditionalScripts[script].Add(new ConditionalScript
                        {
                            Action = del,
                            Name = del.Method.DeclaringType?.Name + "." + del.Method.Name
                        });
                    }
                    // Otherwise it's a normal script.
                    else if (mi.ReturnType == typeof(void))
                    {
                        var del = (Action)mi.CreateDelegate(typeof(Action));

                        if (!_scripts.ContainsKey(script))
                            _scripts[script] = new List<ActionScript>();

                        _scripts[script].Add(new ActionScript
                        {
                            Action = del,
                            Name = del.Method.DeclaringType?.Name + "." + del.Method.Name
                        });
                    }
                    else
                    {
                        Log.Write(LogGroup.Error, $"Method '{mi.Name}' tied to script '{script}' has an invalid return type. This script was NOT loaded.", true);
                    }

                }
            }
        }


        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs ex)
        {
            Log.Write(LogGroup.Error, ((Exception)ex.ExceptionObject).ToMessageAndCompleteStacktrace());
        }

        /// <summary>
        /// Main loop processing called by the new bootstrap
        /// </summary>
        public static void ProcessMainLoop(ulong frame)
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


        /// <summary>
        /// Signal processing called by the new bootstrap
        /// </summary>
        public static void ProcessSignal(string signal)
        {
            // SWLOR doesn't currently handle signals, but this preserves the interface
            // for future expansion if needed
        }

    }
}
