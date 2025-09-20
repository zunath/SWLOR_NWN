using System;
using System.Runtime.InteropServices;
using NWN.Core;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Server;
using SWLOR.Shared.Core.Log;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Core.Server.Contracts;

namespace SWLOR.Shared.Core.Server
{
    public unsafe class NativeInteropManager : INativeInteropManager
    {
        private static readonly ILogger _logger = ServiceContainer.GetService<ILogger>();
        private static readonly IMainLoopProcessor _mainLoopProcessor = ServiceContainer.GetService<IMainLoopProcessor>();
        private static readonly IScriptExecutor _scriptExecutor = ServiceContainer.GetService<IScriptExecutor>();
        private static readonly IClosureManager _closureManager = ServiceContainer.GetService<IClosureManager>();

        public void RegisterHandlers()
        {
            NWNXAPI.RegisterMainLoopHandler(&OnMainLoop);
            NWNXAPI.RegisterRunScriptHandler(&OnRunScript);
            NWNXAPI.RegisterClosureHandler(&OnClosure);
            NWNXAPI.RegisterSignalHandler(&OnSignal);
        }

        [UnmanagedCallersOnly]
        private static void OnMainLoop(ulong frame)
        {
            try
            {
                _mainLoopProcessor.ProcessMainLoop(frame);
            }
            catch (Exception e)
            {
                _logger.Write<ErrorLogGroup>($"MainLoop exception: {e}", true);
            }
        }

        [UnmanagedCallersOnly]
        private static int OnRunScript(IntPtr scriptPtr, uint oidSelf)
        {
            try
            {
                var scriptName = scriptPtr.ReadNullTerminatedString();
                return _scriptExecutor.ProcessRunScript(scriptName, oidSelf);
            }
            catch (Exception e)
            {
                _logger.Write<ErrorLogGroup>($"RunScript exception: {e}", true);
                return -1;
            }
        }

        [UnmanagedCallersOnly]
        private static void OnSignal(IntPtr signalPtr)
        {
            try
            {
                var signal = signalPtr.ReadNullTerminatedString();
                ProcessSignal(signal);
            }
            catch (Exception e)
            {
                _logger.Write<ErrorLogGroup>($"Signal processing exception: {e}", true);
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
                _logger.Write<ErrorLogGroup>($"Closure exception: {e}", true);
            }
        }


        private static void ProcessSignal(string signal)
        {
            switch (signal)
            {
                default:
                    break;
            }
        }
    }
}