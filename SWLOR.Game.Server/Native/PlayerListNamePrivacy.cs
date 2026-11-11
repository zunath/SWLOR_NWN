using System.Runtime.InteropServices;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.NativeSinks;

namespace SWLOR.Game.Server.Native
{
    /// <summary>Installs the hook that lets <see cref="PlayerListNamePrivacySink"/> serialize player-list additions separately for each observer's private names.</summary>
    public static unsafe class PlayerListNamePrivacy
    {
        private static FunctionHook* _addHook;
        private static IPlayerListNamePrivacySink _sink;

        [BootPhase(BootReload.NativeHook)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHook()
        {
            if (_addHook != null) return;
            _sink ??= new PlayerListNamePrivacySink();
            delegate* unmanaged<void*, uint, void*, int> add = &SendPlayerListAdd;
            // Run outside NWNX Rename's Early hook. Its broadcast path applies every
            // observer's name to one creature, then serializes only the last name once.
            _addHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(NativeLibrary.GetMainProgramHandle(),
                "_ZN11CNWSMessage32SendServerToPlayerPlayerList_AddEjP10CNWSPlayer"),
                (IntPtr)add, HookOrder.Earliest);
        }

        [UnmanagedCallersOnly]
        private static int SendPlayerListAdd(void* message, uint recipientId, void* newPlayer)
            => _sink.SendPlayerListAdd(message, recipientId, newPlayer, (nint)_addHook->m_trampoline);
    }
}
