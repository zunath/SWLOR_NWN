using System;
using System.Runtime.InteropServices;
using System.Text;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.Core;

namespace SWLOR.Game.Server.Core
{
    public static unsafe class BootstrapLoader
    {
        private static readonly Encoding Encoding;

        static BootstrapLoader()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding = Encoding.GetEncoding("windows-1252");
        }

        /// <summary>
        /// New bootstrap method called by NWNX DotNET plugin
        /// </summary>
        public static void Bootstrap()
        {
            try
            {
                Console.WriteLine("SWLOR Server starting with new bootstrap method...");
                
                // Register all handlers with the new NWNX API
                NWNXPInvoke.RegisterMainLoopHandler(&OnMainLoop);
                NWNXPInvoke.RegisterRunScriptHandler(&OnRunScript);
                NWNXPInvoke.RegisterClosureHandler(&OnClosure);
                NWNXPInvoke.RegisterSignalHandler(&OnSignal);

                // Initialize the SWLOR internal systems
                Console.WriteLine("Initializing SWLOR internal systems...");
                Internal.Initialize();
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
                Internal.ProcessMainLoop(frame);
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
                return Internal.ProcessRunScript(scriptName, oidSelf);
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
                Internal.ProcessSignal(signal);
            }
            catch (Exception e)
            {
                Log.Write(LogGroup.Error, $"Signal processing exception: {e}");
            }
        }

        [UnmanagedCallersOnly]
        private static void OnClosure(ulong oid, uint eventId)
        {
            try
            {
                // SWLOR doesn't currently use closures directly, but we need this handler
                // for NWNX compatibility. Future closure functionality can be added here.
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

            return Encoding.GetString(bytes);
        }
    }
}
