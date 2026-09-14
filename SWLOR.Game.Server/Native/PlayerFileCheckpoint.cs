using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Native
{
    /// <summary>Persists the migration checkpoint in BIC exports, which omit creature locals.</summary>
    public static unsafe class PlayerFileCheckpoint
    {
        internal const string FieldName = "PlayerFileVer";
        private static readonly byte[] FieldLabel = System.Text.Encoding.ASCII.GetBytes(FieldName + "\0");
        private static FunctionHook* _saveHook;
        private static FunctionHook* _loadHook;

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHooks()
        {
            if (_saveHook != null)
                return;
            delegate* unmanaged<void*, void*, void*, int, int, int, int, int> save = &Save;
            delegate* unmanaged<void*, void*, void*, int, int, int, int, int> load = &Load;
            var program = NativeLibrary.GetMainProgramHandle();
            _saveHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(program,
                "_ZN12CNWSCreature12SaveCreatureEP7CResGFFP10CResStructiiii"), (IntPtr)save, HookOrder.Early);
            _loadHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(program,
                "_ZN12CNWSCreature12LoadCreatureEP7CResGFFP10CResStructiiii"), (IntPtr)load, HookOrder.Early);
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
                var version = CNWSCreature.FromPointer(creature).m_ScriptVars.GetInt(name);
                if (version <= 0)
                    return saved;
                fixed (byte* field = FieldLabel)
                    return CResGFF.FromPointer(resource)
                        .WriteFieldINT(CResStruct.FromPointer(structure), version, field);
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
                var found = 0;
                int version;
                fixed (byte* field = FieldLabel)
                    version = CResGFF.FromPointer(resource)
                        .ReadFieldINT(CResStruct.FromPointer(structure), field, &found, 0);
                if (found != 0 && version > 0)
                {
                    using var name = new CExoString(Migration.PlayerFileVersionVariable);
                    CNWSCreature.FromPointer(creature).m_ScriptVars.SetInt(name, version);
                }
                return loaded;
            }
            catch (Exception exception)
            {
                Log.WriteError(exception, "Character checkpoint deserialization failed");
                return 0;
            }
        }
    }
}
