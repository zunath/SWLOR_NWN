using System.Text;
using NWN.Native.API;
using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Domain.Crafting.Minigame;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Native;

namespace SWLOR.Game.Server.NativeSinks;

/// <summary>
/// Persists the migration checkpoint and the craft file sequence in BIC exports, which omit creature locals. The craft file
/// sequence (<c>CraftFileSeq</c>) is how a login tells whether the character file already holds an item transfer the player
/// record says happened (see <c>CraftSessionLifecycle</c>); it is written only once it is above zero, so files of characters
/// that never crafted carry no extra field.
/// </summary>
public sealed unsafe class PlayerFileCheckpointSink : IPlayerFileCheckpointSink
{
    private static readonly byte[] FieldLabel = Encoding.ASCII.GetBytes(PlayerFileCheckpoint.FieldName + "\0");
    private static readonly byte[] CraftSequenceLabel = Encoding.ASCII.GetBytes(PlayerFileCheckpoint.CraftSequenceFieldName + "\0");

    public int Save(void* creature, void* resource, void* structure,
        int associates, int desiredArea, int exporting, int objectIds, nint original)
    {
        try
        {
            var saved = ((delegate* unmanaged<void*, void*, void*, int, int, int, int, int>)original)(creature, resource, structure, associates, desiredArea, exporting, objectIds);
            if (saved == 0)
                return 0;
            using var name = new CExoString(MigrationService.PlayerFileVersionVariable);
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

    public int Load(void* creature, void* resource, void* structure,
        int saveGame, int associate, int preserveIds, int copyObject, nint original)
    {
        try
        {
            using var inventory = SavedPlayerInventory.BeginLoad(CResGFF.FromPointer(resource), CResStruct.FromPointer(structure));
            using var normalized = SavedPlayerClassLayout.Normalize(CResGFF.FromPointer(resource), CResStruct.FromPointer(structure));
            var loaded = ((delegate* unmanaged<void*, void*, void*, int, int, int, int, int>)original)(creature,
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

    public uint ReadStats(void* stats, void* resource, void* structure, void* appearance,
        int saveGame, int playerCharacter, int copyObject, int loadObjectId, nint original)
    {
        try
        {
            var result = ((delegate* unmanaged<void*, void*, void*, void*, int, int, int, int, uint>)original)(stats, resource, structure, appearance, saveGame, playerCharacter, copyObject, loadObjectId);
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
            using var name = new CExoString(MigrationService.PlayerFileVersionVariable);
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
