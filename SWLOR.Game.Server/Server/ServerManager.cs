using System;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Game.Server.Server
{
    public class ServerManager : IServerManager
    {
        private IServerBootstrapper Bootstrapper { get; }
        private IMainLoopProcessor MainLoop { get; }

        public ServerManager(
            IServerBootstrapper bootstrapper,
            IMainLoopProcessor mainLoop)
        {
            Bootstrapper = bootstrapper;
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
