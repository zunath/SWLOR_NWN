using System;
using System.Collections.Generic;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.NWN.API.NWNX;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using ItemPropertyType = SWLOR.NWN.API.NWScript.Enum.Item.ItemPropertyType;

namespace SWLOR.Game.Server.Feature
{
    /// <summary>
    /// NWN hardcodes weapon attachment behavior by native base-item ID. Base item 11 always
    /// uses the bow attachment, so its model suppresses a shield even when the 2DA row is made
    /// one-handed. Canonical player pistols use native sling ID 61 instead. Conversion is
    /// permanent and independent of the off-hand item. Native sling attacks also require
    /// ammunition in the bullet slot, so legacy arrow-based blaster ammunition is normalized
    /// to bullets at the same compatibility boundary.
    /// </summary>
    public static class PistolBaseItemCompatibility
    {
        private const string RepairedGeneratedAmmoVariable = "PISTOL_REPAIRED_GENERATED_AMMO";

        private static readonly HashSet<string> LegacySmallArmsResrefs =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "blast_se14_d",
                "blast_jawa_d",
                "dualpistolmain",
                "extjawa004_wp",
                "jawa_wp",
                "jawaaddit_wp",
            };

        [NWNEventHandler(ScriptName.OnModuleAcquire)]
        public static void OnAcquire()
        {
            Normalize(GetModuleItemAcquired());
        }

        [NWNEventHandler(ScriptName.OnItemEquipValidateAfter)]
        public static void OnItemEquip()
        {
            Normalize(GetItemInSlot(InventorySlot.RightHand, OBJECT_SELF));
        }

        [NWNEventHandler(ScriptName.OnItemUnequipAfter)]
        public static void OnItemUnequip()
        {
            var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
            if (!GetIsObjectValid(item) || GetItemInSlot(InventorySlot.RightHand, OBJECT_SELF) == item)
                return;

            var generatedAmmoId = GetLocalObject(item, RepairedGeneratedAmmoVariable);
            if (!GetIsObjectValid(generatedAmmoId))
                return;

            DeleteLocalObject(item, RepairedGeneratedAmmoVariable);
            var nativeCreature = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(OBJECT_SELF)?.AsNWSCreature();
            var generatedAmmo = nativeCreature?.m_pInventory.GetItemInSlot(1u << (int)InventorySlot.Bullets);
            if (nativeCreature == null || nativeCreature.m_bMagicalBulletsEquipped == 0 ||
                generatedAmmo?.m_idSelf != generatedAmmoId)
                return;

            // Saved legacy weapons can leave the moved stack behind during native cleanup.
            // Release only the engine-owned stack moved by this weapon's repair.
            if (nativeCreature.m_pInventory.RemoveItem(generatedAmmo) == 0)
                throw new InvalidOperationException("Unable to release repaired pistol ammunition on unequip.");
            nativeCreature.m_bMagicalBulletsEquipped = 0;
            DestroyObject(generatedAmmoId);
        }

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void OnClientEnter()
        {
            var creature = GetEnteringObject();
            if (!GetIsPC(creature) || GetIsDM(creature))
                return;

            var nativeCreature = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(creature)?.AsNWSCreature();
            // Generated stacks are moved by Normalize; the queued slot migration below
            // handles only real saved ammunition that must remain in the player's inventory.
            var equippedLegacyAmmo = (nativeCreature?.m_bMagicalArrowsEquipped ?? 0) != 0
                ? OBJECT_INVALID
                : GetItemInSlot(InventorySlot.Arrows, creature);
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

            var normalizedLegacyAmmoType = GetIsObjectValid(equippedLegacyAmmo)
                ? GetBaseItemType(equippedLegacyAmmo)
                : BaseItem.Invalid;
            if (normalizedLegacyAmmoType == BaseItem.Bullet)
            {
                var equippedBulletAmmo = GetItemInSlot(InventorySlot.Bullets, creature);
                var clearOccupiedBulletSlot = ShouldClearBulletSlot(
                    normalizedLegacyAmmoType,
                    GetIsObjectValid(equippedBulletAmmo));

                AssignCommand(
                    creature,
                    () =>
                    {
                        if (clearOccupiedBulletSlot && GetIsObjectValid(equippedBulletAmmo))
                            ActionUnequipItem(equippedBulletAmmo);

                        ActionEquipItem(equippedLegacyAmmo, InventorySlot.Bullets);
                    });
            }
        }

        public static bool ShouldClearBulletSlot(
            BaseItem normalizedLegacyAmmoType,
            bool bulletSlotOccupied)
        {
            return normalizedLegacyAmmoType == BaseItem.Bullet && bulletSlotOccupied;
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

            if (currentBaseItem == BaseItem.Arrow)
                return BaseItem.Bullet;

            return currentBaseItem == BaseItem.Pistol ||
                   currentBaseItem == BaseItem.LegacyPistol
                ? BaseItem.Sling
                : currentBaseItem;
        }

        public static InventorySlot GetCanonicalInventorySlot(
            BaseItem currentBaseItem,
            InventorySlot requestedSlot)
        {
            return currentBaseItem == BaseItem.Arrow &&
                   requestedSlot == InventorySlot.Arrows
                ? InventorySlot.Bullets
                : requestedSlot;
        }

        public static bool ShouldRepairGeneratedPistolAmmunition(
            BaseItem currentBaseItem,
            BaseItem canonicalBaseItem,
            bool isEquipped,
            bool hasUnlimitedAmmunition,
            bool generatedArrows,
            bool generatedBullets)
        {
            return isEquipped && hasUnlimitedAmmunition && generatedArrows && !generatedBullets &&
                   canonicalBaseItem == BaseItem.Sling &&
                   (currentBaseItem == BaseItem.Pistol ||
                    currentBaseItem == BaseItem.LegacyPistol ||
                    currentBaseItem == BaseItem.Sling);
        }

        public static bool Normalize(uint item)
        {
            if (!GetIsObjectValid(item))
                return false;

            var currentBaseItem = GetBaseItemType(item);
            var canonicalBaseItem = GetCanonicalBaseItem(currentBaseItem, GetResRef(item));

            var shouldCheckGeneratedAmmo = canonicalBaseItem == BaseItem.Sling &&
                                           (currentBaseItem == BaseItem.Pistol ||
                                            currentBaseItem == BaseItem.LegacyPistol ||
                                            currentBaseItem == BaseItem.Sling);
            if (shouldCheckGeneratedAmmo && HasUnlimitedAmmunition(item))
            {
                var nativeItem = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(item)?.AsNWSItem();
                var possessor = nativeItem?.m_oidPossessor ?? OBJECT_INVALID;
                var nativeCreature = GetIsObjectValid(possessor)
                    ? NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(possessor)?.AsNWSCreature()
                    : null;
                var equippedSlot = nativeCreature != null && nativeItem != null
                    ? nativeCreature.m_pInventory.GetSlotFromItem(nativeItem)
                    : 0;
                var rightHandMask = 1u << (int)InventorySlot.RightHand;
                var isEquippedRightHand = nativeCreature != null && nativeItem != null &&
                                          equippedSlot == rightHandMask &&
                                          nativeCreature.m_pInventory.GetItemInSlot(equippedSlot)?.m_idSelf == item;

                if (nativeCreature != null && nativeItem != null && isEquippedRightHand)
                {
                    if (ShouldRepairGeneratedPistolAmmunition(
                            currentBaseItem,
                            canonicalBaseItem,
                            true,
                            true,
                            nativeCreature.m_bMagicalArrowsEquipped != 0,
                            nativeCreature.m_bMagicalBulletsEquipped != 0))
                    {
                        var arrowSlot = 1u << (int)InventorySlot.Arrows;
                        var generatedAmmo = nativeCreature.m_pInventory.GetItemInSlot(arrowSlot);
                        if (generatedAmmo == null || generatedAmmo.m_oidPossessor != possessor ||
                            generatedAmmo.m_nBaseItem != (int)BaseItem.Arrow &&
                            generatedAmmo.m_nBaseItem != (int)BaseItem.Bullet)
                            throw new InvalidOperationException("Unable to identify generated legacy pistol ammunition.");

                        var bulletSlot = 1u << (int)InventorySlot.Bullets;
                        var realAmmo = nativeCreature.m_pInventory.GetItemInSlot(bulletSlot);
                        if (realAmmo != null)
                        {
                            var ammoId = realAmmo.m_idSelf;
                            var quantity = GetItemStackSize(ammoId);
                            CreaturePlugin.RunUnequip(possessor, ammoId);
                            if (nativeCreature.m_pInventory.GetItemInSlot(bulletSlot)?.m_idSelf == ammoId ||
                                GetItemPossessor(ammoId) != possessor || GetItemStackSize(ammoId) != quantity ||
                                nativeCreature.m_pcItemRepository.GetItemInRepository(realAmmo) == 0)
                                throw new InvalidOperationException("Unable to preserve real bullets while repairing pistol ammunition.");
                        }

                        // Load can generate arrows before acquire handling changes the pistol's base.
                        // Move that engine-owned stack and its ownership flag together; cycling
                        // a saved legacy weapon alone can regenerate ammunition in the old slot.
                        if (nativeCreature.m_pInventory.RemoveItem(generatedAmmo) == 0)
                            throw new InvalidOperationException("Unable to release generated legacy pistol ammunition.");
                        ItemPlugin.SetBaseItemType(item, canonicalBaseItem);
                        ItemPlugin.SetBaseItemType(generatedAmmo.m_idSelf, BaseItem.Bullet);
                        nativeCreature.m_pInventory.PutItemInSlot(bulletSlot, generatedAmmo);
                        nativeCreature.m_bMagicalArrowsEquipped = 0;
                        nativeCreature.m_bMagicalBulletsEquipped = 1;
                        SetLocalObject(item, RepairedGeneratedAmmoVariable, generatedAmmo.m_idSelf);
                        nativeCreature.UpdateAppearanceForEquippedItems();
                        return true;
                    }
                }
            }

            if (canonicalBaseItem == currentBaseItem)
                return false;

            ItemPlugin.SetBaseItemType(item, canonicalBaseItem);
            return true;
        }

        private static bool HasUnlimitedAmmunition(uint item)
        {
            for (var property = GetFirstItemProperty(item);
                 GetIsItemPropertyValid(property);
                 property = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(property) == ItemPropertyType.UnlimitedAmmunition)
                    return true;
            }

            return false;
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
