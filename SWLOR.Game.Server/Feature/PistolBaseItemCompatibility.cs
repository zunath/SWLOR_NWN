using System;
using System.Collections.Generic;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.NWN.API.NWNX;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;

namespace SWLOR.Game.Server.Feature
{
    /// <summary>
    /// NWN hardcodes weapon attachment behavior by native base-item ID. Base item 11 always
    /// uses the bow attachment, so its model suppresses a shield even when the 2DA row is made
    /// one-handed. Canonical player pistols use native sling ID 61 instead. Conversion is
    /// permanent and independent of the off-hand item.
    /// </summary>
    public static class PistolBaseItemCompatibility
    {
        private static readonly HashSet<string> LegacySmallArmsResrefs =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "blast_jawa_d",
                "dualpistolmain",
            };

        [NWNEventHandler(ScriptName.OnModuleAcquire)]
        public static void OnAcquire()
        {
            Normalize(GetModuleItemAcquired());
        }

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void OnClientEnter()
        {
            var creature = GetEnteringObject();
            if (!GetIsPC(creature) || GetIsDM(creature))
                return;

            var equippedItemChanged = false;
            for (var index = 0; index < NumberOfInventorySlots; index++)
            {
                var item = GetItemInSlot((InventorySlot)index, creature);
                equippedItemChanged |= Normalize(item);
            }

            for (var item = GetFirstItemInInventory(creature);
                 GetIsObjectValid(item);
                 item = GetNextItemInInventory(creature))
            {
                Normalize(item);
            }

            if (equippedItemChanged)
                RefreshEquippedItemAppearance(creature);
        }

        public static BaseItem GetCanonicalBaseItem(BaseItem currentBaseItem, string resref)
        {
            if (LegacySmallArmsResrefs.Contains(resref))
            {
                return currentBaseItem == BaseItem.Pistol ||
                       currentBaseItem == BaseItem.Sling ||
                       currentBaseItem == BaseItem.LegacyPistol
                    ? BaseItem.LegacyPistol
                    : currentBaseItem;
            }

            return currentBaseItem == BaseItem.Pistol ||
                   currentBaseItem == BaseItem.LegacyPistol
                ? BaseItem.Sling
                : currentBaseItem;
        }

        public static bool Normalize(uint item)
        {
            if (!GetIsObjectValid(item))
                return false;

            var currentBaseItem = GetBaseItemType(item);
            var canonicalBaseItem = GetCanonicalBaseItem(currentBaseItem, GetResRef(item));
            if (canonicalBaseItem == currentBaseItem)
                return false;

            ItemPlugin.SetBaseItemType(item, canonicalBaseItem);
            return true;
        }

        private static void RefreshEquippedItemAppearance(uint creature)
        {
            var creaturePointer = NWNXUtils.GetGameObject(creature);
            if (creaturePointer == nint.Zero)
                return;

            var nativeCreature = CNWSCreature.FromPointer(creaturePointer);
            nativeCreature?.UpdateAppearanceForEquippedItems();
        }
    }
}
