using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using System;
using System.Runtime.InteropServices;
using System.Text;
using NWNX.NET;
using NWNX.NET.Native;

namespace SWLOR.Game.Server.Core
{
    public unsafe class NativeInteropManager
    {
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
                ServerManager.MainLoop.ProcessMainLoop(frame);
            }
            catch (Exception e)
            {
                Log.Write(LogGroup.Error, $"MainLoop exception: {e}", true);
            }
        }

        [UnmanagedCallersOnly]
        private static int OnRunScript(IntPtr scriptPtr, uint oidSelf)
        {
            try
            {
                var scriptName = scriptPtr.ReadNullTerminatedString();
                return ServerManager.Executor.ProcessRunScript(scriptName, oidSelf);
            }
            catch (Exception e)
            {
                Log.Write(LogGroup.Error, $"RunScript exception: {e}", true);
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
                Log.Write(LogGroup.Error, $"Signal processing exception: {e}", true);
            }
        }

        [UnmanagedCallersOnly]
        private static void OnClosure(ulong eid, uint oidSelf)
        {
            try
            {
                ServerManager.Bootstrapper.ClosureManager.OnClosure(eid, oidSelf);
            }
            catch (Exception e)
            {
                Log.Write(LogGroup.Error, $"Closure processing exception: {e}", true);
            }
        }


        private static void ProcessSignal(string signal)
        {
            switch (signal)
            {
                case "ON_MODULE_LOAD_FINISH":
                    ServerManager.Executor.Initialize();
                    break;
            }
        }
    }
}