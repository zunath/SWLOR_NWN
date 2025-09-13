using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.Core;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SWLOR.Game.Server.Core
{
    public unsafe class NativeInteropManager
    {
        private static readonly Encoding _encoding;

        static NativeInteropManager()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            _encoding = Encoding.GetEncoding("windows-1252");
        }

        public void RegisterHandlers()
        {
            NWNXPInvoke.RegisterMainLoopHandler(&OnMainLoop);
            NWNXPInvoke.RegisterRunScriptHandler(&OnRunScript);
            NWNXPInvoke.RegisterClosureHandler(&OnClosure);
            NWNXPInvoke.RegisterSignalHandler(&OnSignal);
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
        private static int OnRunScript(byte* pScript, uint oidSelf)
        {
            try
            {
                string scriptName = ReadNullTerminatedString(pScript);
                return ServerManager.Executor.ProcessRunScript(scriptName, oidSelf);
            }
            catch (Exception e)
            {
                Log.Write(LogGroup.Error, $"RunScript exception: {e}", true);
                return -1;
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

        private static void ProcessSignal(string signal)
        {
            // SWLOR doesn't currently handle signals, but this preserves the interface
            // for future expansion if needed
        }
    }
}