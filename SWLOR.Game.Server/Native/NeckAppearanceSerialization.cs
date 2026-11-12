using System;
using System.Runtime.InteropServices;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.NativeSinks;

namespace SWLOR.Game.Server.Native
{
    /// <summary>Installs the hook that lets <see cref="NeckAppearanceSerializationSink"/> project the equipped neck, shoulder and part-skin models into the appearance the engine sends.</summary>
    public static unsafe class NeckAppearanceSerialization
    {
        private static FunctionHook* _hook;
        private static INeckAppearanceSerializationSink _sink;

        [BootPhase(BootReload.NativeHook)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHook()
        {
            if (_hook != null) return;
            _sink ??= new NeckAppearanceSerializationSink();
            delegate* unmanaged<void*, void*, void*, void> write = &Write;
            _hook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(NativeLibrary.GetMainProgramHandle(),
                "_ZN11CNWSMessage26AddItemAppearanceToMessageEP10CNWSPlayerP8CNWSItem"),
                (IntPtr)write, HookOrder.Late);
        }

        [UnmanagedCallersOnly]
        private static void Write(void* message, void* player, void* itemPointer)
            => _sink.Write(message, player, itemPointer, (nint)_hook->m_trampoline);
    }
}
