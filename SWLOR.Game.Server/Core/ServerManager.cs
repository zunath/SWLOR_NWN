using System;

namespace SWLOR.Game.Server.Core
{
    public static class ServerManager
    {
        public static ServerBootstrapper Bootstrapper { get; private set; }
        public static NativeInteropManager NativeInterop { get; private set; }
        public static ScriptRegistry Scripts { get; private set; }
        public static ScriptExecutor Executor { get; private set; }
        public static MainLoopProcessor MainLoop { get; private set; }

        static ServerManager()
        {
            InitializeComponents();
        }

        private static void InitializeComponents()
        {
            Bootstrapper = new ServerBootstrapper();
            NativeInterop = new NativeInteropManager();
            Scripts = new ScriptRegistry();
            Executor = new ScriptExecutor();
            MainLoop = new MainLoopProcessor();
        }

        public static event Action OnScriptContextBegin
        {
            add => MainLoopProcessor.OnScriptContextBegin += value;
            remove => MainLoopProcessor.OnScriptContextBegin -= value;
        }

        public static event Action OnScriptContextEnd
        {
            add => MainLoopProcessor.OnScriptContextEnd += value;
            remove => MainLoopProcessor.OnScriptContextEnd -= value;
        }

        /// <summary>
        /// New bootstrap method called by NWNX DotNET plugin
        /// </summary>
        public static void Bootstrap()
        {
            Bootstrapper.Bootstrap();
        }
    }
}
