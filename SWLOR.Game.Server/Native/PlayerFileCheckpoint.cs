using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.NativeSinks;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// Persists the migration checkpoint and the craft file sequence in BIC exports, which omit creature locals. The craft file
    /// sequence (<c>CraftFileSeq</c>) is how a login tells whether the character file already holds an item transfer the player
    /// record says happened (see <c>CraftSessionLifecycle</c>); it is written only once it is above zero, so files of characters
    /// that never crafted carry no extra field.
    /// </summary>
    public static unsafe class PlayerFileCheckpoint
    {
        internal const string FieldName = "PlayerFileVer";
        internal const string CraftSequenceFieldName = "CraftFileSeq";
        private static FunctionHook* _saveHook;
        private static FunctionHook* _loadHook;
        private static FunctionHook* _readStatsHook;
        private static IPlayerFileCheckpointSink _sink;

        [BootPhase(BootReload.NativeHook)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHooks()
        {
            if (_saveHook != null)
                return;
            _sink ??= new PlayerFileCheckpointSink();
            delegate* unmanaged<void*, void*, void*, int, int, int, int, int> save = &Save;
            delegate* unmanaged<void*, void*, void*, int, int, int, int, int> load = &Load;
            delegate* unmanaged<void*, void*, void*, void*, int, int, int, int, uint> readStats = &ReadStats;
            var program = NativeLibrary.GetMainProgramHandle();
            _saveHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(program,
                "_ZN12CNWSCreature12SaveCreatureEP7CResGFFP10CResStructiiii"), (IntPtr)save, HookOrder.Early);
            _loadHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(program,
                "_ZN12CNWSCreature12LoadCreatureEP7CResGFFP10CResStructiiii"), (IntPtr)load, HookOrder.Early);
            _readStatsHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(program,
                "_ZN17CNWSCreatureStats16ReadStatsFromGffEP7CResGFFP10CResStructP26CNWSCreatureAppearanceInfoiiii"),
                (IntPtr)readStats, HookOrder.Early);
        }

        [UnmanagedCallersOnly]
        private static int Save(void* creature, void* resource, void* structure,
            int associates, int desiredArea, int exporting, int objectIds)
            => _sink.Save(creature, resource, structure, associates, desiredArea, exporting, objectIds,
                (nint)_saveHook->m_trampoline);

        [UnmanagedCallersOnly]
        private static int Load(void* creature, void* resource, void* structure,
            int saveGame, int associate, int preserveIds, int copyObject)
            => _sink.Load(creature, resource, structure, saveGame, associate, preserveIds, copyObject,
                (nint)_loadHook->m_trampoline);

        [UnmanagedCallersOnly]
        private static uint ReadStats(void* stats, void* resource, void* structure, void* appearance,
            int saveGame, int playerCharacter, int copyObject, int loadObjectId)
            => _sink.ReadStats(stats, resource, structure, appearance, saveGame, playerCharacter, copyObject,
                loadObjectId, (nint)_readStatsHook->m_trampoline);
    }
}
