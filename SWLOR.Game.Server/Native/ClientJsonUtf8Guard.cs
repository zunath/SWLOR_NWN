using System.Collections.Generic;
using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.NativeSinks;
using SWLOR.Game.Server.Domain.Foundation;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// Makes the JSON that clients send with NUI events and bind updates safe for the engine to decode and log.
    /// </summary>
    public static unsafe class ClientJsonUtf8Guard
    {
        private const string ReadJsonSymbol = "_ZN10CNWMessage8ReadJSONB5cxx11Ev";

        private static FunctionHook* _readJsonHook;
        private static IClientJsonUtf8GuardSink _sink;

        /// <summary>The number of invalid UTF-8 bytes replaced since the module loaded.</summary>
        public static long ReplacedByteCount { get; internal set; }

        /// <summary>The number of client JSON payloads made unreadable since the module loaded.</summary>
        public static long RejectedPayloadCount { get; internal set; }

        [BootPhase(BootReload.NativeHook)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHook()
        {
            if (_readJsonHook != null)
                return;

            _sink ??= new ClientJsonUtf8GuardSink();
            delegate* unmanaged<void*, void*, void*> readJson = &ReadJson;
            _readJsonHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(NativeLibrary.GetMainProgramHandle(), ReadJsonSymbol),
                (IntPtr)readJson, HookOrder.Earliest);
        }

        // ReadJSON returns the decoded json through a hidden result pointer, ahead of the message pointer.
        [UnmanagedCallersOnly]
        private static void* ReadJson(void* result, void* message)
            => _sink.ReadJson(result, message, (nint)_readJsonHook->m_trampoline);
    }
}
