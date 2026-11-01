using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService.Minigame;

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
        private static readonly byte[] FieldLabel = System.Text.Encoding.ASCII.GetBytes(FieldName + "\0");
        private static readonly byte[] CraftSequenceLabel = System.Text.Encoding.ASCII.GetBytes(CraftSequenceFieldName + "\0");
        private static FunctionHook* _saveHook;
        private static FunctionHook* _loadHook;
        private static FunctionHook* _readStatsHook;

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHooks()
        {
            if (_saveHook != null)
                return;
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
        {
            try
            {
                var original = (delegate* unmanaged<void*, void*, void*, int, int, int, int, int>)_saveHook->m_trampoline;
                var saved = original(creature, resource, structure, associates, desiredArea, exporting, objectIds);
                if (saved == 0)
                    return 0;
                using var name = new CExoString(Migration.PlayerFileVersionVariable);
                var scriptVars = CNWSCreature.FromPointer(creature).m_ScriptVars;
                var version = scriptVars.GetInt(name);
                if (version > 0)
                {
                    fixed (byte* field = FieldLabel)
                        saved = CResGFF.FromPointer(resource)
                            .WriteFieldINT(CResStruct.FromPointer(structure), version, field);
                }

                if (saved == 0)
                    return 0;

                using var craftName = new CExoString(CraftSessionFileSequence.Variable);
                var craftSequence = scriptVars.GetInt(craftName);
                if (craftSequence <= 0)
                    return saved;
                fixed (byte* field = CraftSequenceLabel)
                    return CResGFF.FromPointer(resource)
                        .WriteFieldINT(CResStruct.FromPointer(structure), craftSequence, field);
            }
            catch (Exception exception)
            {
                Log.WriteError(exception, "Character checkpoint serialization failed");
                return 0;
            }
        }

        [UnmanagedCallersOnly]
        private static int Load(void* creature, void* resource, void* structure,
            int saveGame, int associate, int preserveIds, int copyObject)
        {
            try
            {
                var original = (delegate* unmanaged<void*, void*, void*, int, int, int, int, int>)_loadHook->m_trampoline;
                using var inventory = SavedPlayerInventory.BeginLoad(CResGFF.FromPointer(resource), CResStruct.FromPointer(structure));
                using var normalized = SavedPlayerClassLayout.Normalize(CResGFF.FromPointer(resource), CResStruct.FromPointer(structure));
                var loaded = original(creature,
                    normalized == null ? resource : (void*)normalized.File.Pointer,
                    normalized == null ? structure : (void*)normalized.Root.Pointer,
                    saveGame, associate, preserveIds, copyObject);
                if (loaded == 0)
                    return 0;
                RestoreCheckpoint(CNWSCreature.FromPointer(creature), resource, structure);
                return loaded;
            }
            catch (Exception exception)
            {
                Log.WriteError(exception, "Character checkpoint deserialization failed");
                return 0;
            }
        }

        [UnmanagedCallersOnly]
        private static uint ReadStats(void* stats, void* resource, void* structure, void* appearance,
            int saveGame, int playerCharacter, int copyObject, int loadObjectId)
        {
            try
            {
                var original = (delegate* unmanaged<void*, void*, void*, void*, int, int, int, int, uint>)_readStatsHook->m_trampoline;
                var result = original(stats, resource, structure, appearance, saveGame, playerCharacter, copyObject, loadObjectId);
                // CNWSPlayer.LoadCreatureData bypasses LoadCreature and reads stats directly.
                // Its return convention is zero on success, unlike LoadCreature.
                if (result == 0 && playerCharacter != 0)
                    RestoreCheckpoint(CNWSCreatureStats.FromPointer(stats).m_pBaseCreature, resource, structure);
                return result;
            }
            catch (Exception exception)
            {
                Log.WriteError(exception, "Player login checkpoint deserialization failed");
                return uint.MaxValue;
            }
        }

        private static void RestoreCheckpoint(CNWSCreature creature, void* resource, void* structure)
        {
            var found = 0;
            int version;
            fixed (byte* field = FieldLabel)
                version = CResGFF.FromPointer(resource)
                    .ReadFieldINT(CResStruct.FromPointer(structure), field, &found, 0);
            if (found != 0 && version > 0)
            {
                using var name = new CExoString(Migration.PlayerFileVersionVariable);
                creature.m_ScriptVars.SetInt(name, version);
            }

            // The sequence loaded from the file is also what the file is known to hold, until this session exports again.
            found = 0;
            int craftSequence;
            fixed (byte* field = CraftSequenceLabel)
                craftSequence = CResGFF.FromPointer(resource)
                    .ReadFieldINT(CResStruct.FromPointer(structure), field, &found, 0);
            if (found != 0 && craftSequence > 0)
            {
                using var sequenceName = new CExoString(CraftSessionFileSequence.Variable);
                creature.m_ScriptVars.SetInt(sequenceName, craftSequence);
                using var exportedName = new CExoString(CraftSessionFileSequence.ExportedVariable);
                creature.m_ScriptVars.SetInt(exportedName, craftSequence);
                using var loadedName = new CExoString(CraftSessionFileSequence.LoadedVariable);
                creature.m_ScriptVars.SetInt(loadedName, craftSequence);
            }
        }
    }
}
