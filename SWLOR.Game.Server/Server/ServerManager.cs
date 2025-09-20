using System;
using SWLOR.Shared.Core.Server.Contracts;

namespace SWLOR.Shared.Core.Server
{
    public class ServerManager : IServerManager
    {
        public IServerBootstrapper Bootstrapper { get; }
        public INativeInteropManager NativeInterop { get; }
        public IScriptRegistry Scripts { get; }
        public IScriptExecutor Executor { get; }
        public IMainLoopProcessor MainLoop { get; }

        public ServerManager(
            IServerBootstrapper bootstrapper,
            INativeInteropManager nativeInterop,
            IScriptRegistry scriptRegistry,
            IScriptExecutor scriptExecutor,
            IMainLoopProcessor mainLoop)
        {
            Bootstrapper = bootstrapper;
            NativeInterop = nativeInterop;
            Scripts = scriptRegistry;
            Executor = scriptExecutor;
            MainLoop = mainLoop;
        }

        public event Action OnScriptContextBegin
        {
            add => MainLoop.OnScriptContextBegin += value;
            remove => MainLoop.OnScriptContextBegin -= value;
        }

        public event Action OnScriptContextEnd
        {
            add => MainLoop.OnScriptContextEnd += value;
            remove => MainLoop.OnScriptContextEnd -= value;
        }

        /// <summary>
        /// New bootstrap method called by NWNX DotNET plugin
        /// </summary>
        public void Bootstrap()
        {
            Bootstrapper.Bootstrap();
        }
    }
}
