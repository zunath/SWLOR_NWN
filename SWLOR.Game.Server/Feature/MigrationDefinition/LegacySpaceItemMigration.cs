using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>
/// The live twin of the saved-data conversion of retired ship deeds and modules (<see cref="LegacySpaceItemConversion"/>),
/// for items a player picks up from a stale copy after the upgrade: the item is saved, converted the same way and
/// loaded back in its place. A configuration module is destroyed and its value paid to the player.
/// </summary>
internal static class LegacySpaceItemMigration
{
    /// <summary>
    /// Converts the retired items an object holds (or the object itself, when it is an item).
    /// <paramref name="replacedRoot"/> is true when the object itself was replaced or destroyed.
    /// </summary>
    public static bool MigrateObject(uint obj, out bool replacedRoot)
    {
        replacedRoot = false;
        if (!GetIsObjectValid(obj))
            return false;
        if (GetObjectType(obj) == ObjectType.Item && LegacySpaceItemCatalog.IsLegacySpaceItem(GetResRef(obj)))
        {
            replacedRoot = true;
            return MigrateItem(obj);
        }
        if (!GetHasInventory(obj))
            return false;

        // Replacing an item moves the native inventory cursor, so take every entry first.
        var inventory = new List<uint>();
        for (var item = GetFirstItemInInventory(obj); GetIsObjectValid(item); item = GetNextItemInInventory(obj))
            inventory.Add(item);
        var changed = false;
        foreach (var item in inventory)
            changed |= MigrateObject(item, out _);
        return changed;
    }

    private static bool MigrateItem(uint item)
    {
        var holder = GetItemPossessor(item, true);
        var owner = GetItemPossessor(item);
        var player = GetIsPC(owner) && !GetIsDM(owner) ? owner : OBJECT_INVALID;
        var resref = GetResRef(item);
        using var disposal = new MigrationItemDisposal();

        if (LegacySpaceItemCatalog.TryGetConfigurationModule(resref, out var credits))
        {
            var name = GetName(item);
            disposal.Remove(item);
            if (GetIsObjectValid(player))
            {
                GiveGoldToCreature(player, credits);
                SendMessageToPC(player, $"{name} was retired, since ship roles are now built into each hull. You received {credits} credits for it.");
            }
            return true;
        }

        if (!LegacySpaceItemBlueprints.IsLoaded)
            LegacySpaceItemBlueprints.Load();
        var tally = new LegacySpaceItemTally();
        if (!StoredObjectData.MigrateLegacySpaceItems(ObjectPlugin.Serialize(item), out var migrated, tally))
            return false;
        var oldName = GetName(item);
        disposal.Remove(item);
        var replacement = ObjectPlugin.Deserialize(migrated);
        if (GetIsObjectValid(holder) && !ObjectPlugin.AcquireItem(holder, replacement) && GetIsObjectValid(owner))
        {
            CopyObject(replacement, GetLocation(owner));
            DestroyObject(replacement);
        }

        if (GetIsObjectValid(player))
        {
            SendMessageToPC(player, $"{oldName} was converted to {tally.RootReplacement.Name}.");
            if (tally.HullDesigns.Count > 0)
            {
                var playerId = GetObjectUUID(player);
                var dbPlayer = DB.Get<Player>(playerId);
                if (dbPlayer != null && LegacySpaceItemSweep.Pay(dbPlayer, 0, tally.HullDesigns))
                    DB.Set(dbPlayer);
            }
        }
        return true;
    }
}
