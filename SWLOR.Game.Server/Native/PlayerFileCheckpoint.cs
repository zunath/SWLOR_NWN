using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.Native
{
    /// <summary>Persists character checkpoints in BIC exports, which omit creature locals.</summary>
    public static unsafe class PlayerFileCheckpoint
    {
        internal const string FieldName = "PlayerFileVer";
        private static readonly byte[] FieldLabel = System.Text.Encoding.ASCII.GetBytes(FieldName + "\0");
        private static readonly byte[] FoodClaimFieldLabel = System.Text.Encoding.ASCII.GetBytes("FoodClaim\0");
        private static readonly byte[] FoodBatchFieldLabel = System.Text.Encoding.ASCII.GetBytes("FoodBatch\0");
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
                    if (saved == 0)
                        return 0;
                }

                using var foodClaimName = new CExoString(CraftedFoodClaim.CheckpointVariable);
                using var foodClaim = scriptVars.GetString(foodClaimName);
                if (!string.IsNullOrEmpty(foodClaim.ToString()))
                {
                    fixed (byte* field = FoodClaimFieldLabel)
                        saved = CResGFF.FromPointer(resource)
                            .WriteFieldCExoString(CResStruct.FromPointer(structure), foodClaim, field);
                    if (saved == 0)
                        return 0;
                }

                using var foodBatchName = new CExoString(CraftedFoodDelivery.RecoveryVariable);
                using var foodBatch = scriptVars.GetString(foodBatchName);
                if (!string.IsNullOrEmpty(foodBatch.ToString()))
                {
                    fixed (byte* field = FoodBatchFieldLabel)
                        return CResGFF.FromPointer(resource)
                            .WriteFieldCExoString(CResStruct.FromPointer(structure), foodBatch, field);
                }
                return saved;
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

            using var foodClaimName = new CExoString(CraftedFoodClaim.CheckpointVariable);
            using var empty = new CExoString(string.Empty);
            found = 0;
            fixed (byte* field = FoodClaimFieldLabel)
            {
                using var foodClaim = CResGFF.FromPointer(resource)
                    .ReadFieldCExoString(CResStruct.FromPointer(structure), field, &found, empty);
                if (found != 0 && !string.IsNullOrEmpty(foodClaim.ToString()))
                    creature.m_ScriptVars.SetString(foodClaimName, foodClaim);
                else
                    creature.m_ScriptVars.DestroyString(foodClaimName);
            }

            using var foodBatchName = new CExoString(CraftedFoodDelivery.RecoveryVariable);
            found = 0;
            fixed (byte* field = FoodBatchFieldLabel)
            {
                using var foodBatch = CResGFF.FromPointer(resource)
                    .ReadFieldCExoString(CResStruct.FromPointer(structure), field, &found, empty);
                if (found != 0 && !string.IsNullOrEmpty(foodBatch.ToString()))
                    creature.m_ScriptVars.SetString(foodBatchName, foodBatch);
                else
                    creature.m_ScriptVars.DestroyString(foodBatchName);
            }
        }
    }
}
