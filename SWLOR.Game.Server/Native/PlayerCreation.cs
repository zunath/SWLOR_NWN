using System.Runtime.InteropServices;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.NativeSinks;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// Installs the hook that lets <see cref="PlayerCreationSink"/> authorize initial setup only after the engine
    /// accepts a new server character.
    /// </summary>
    public static unsafe class PlayerCreation
    {
        private static FunctionHook* _creationHook;
        private static IPlayerCreationSink _sink;

        [BootPhase(BootReload.NativeHook)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHook()
        {
            if (_creationHook != null) return;
            _sink ??= new PlayerCreationSink();
            delegate* unmanaged<void*, void*, void*, uint, uint> validate = &Validate;
            _creationHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(NativeLibrary.GetMainProgramHandle(),
                "_ZN21CServerExoAppInternal29ValidateCreateServerCharacterEP10CNWSPlayerPvj"),
                (IntPtr)validate, HookOrder.Early);
        }

        [UnmanagedCallersOnly]
        private static uint Validate(void* server, void* client, void* data, uint size)
            => _sink.Validate(server, client, data, size, (nint)_creationHook->m_trampoline);
    }
}
