using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Entity;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service
{
    public static class DualPistolService
    {

        private static uint CopyWeaponAppearance(uint player, uint sourceItem, uint destinationItem, bool copyPropsAndVars)
        {
            var playerId = GetObjectUUID(player);
            var tempStorage = (GetObjectByTag("OUTFIT_BARREL"));
            SetLocalString(sourceItem, "TEMP_OUTFIT_UUID", playerId);
            
            var copiedItem = CopyItem(destinationItem, tempStorage, true);
            copiedItem = CopyItemAndModify(copiedItem, ItemAppearanceType.WeaponModel, 0, (int)GetItemAppearance(sourceItem, ItemAppearanceType.WeaponModel, 0), true);
            copiedItem = CopyItemAndModify(copiedItem, ItemAppearanceType.WeaponColor, 0, (int)GetItemAppearance(sourceItem, ItemAppearanceType.WeaponColor, 0), true);

            copiedItem = CopyItemAndModify(copiedItem, ItemAppearanceType.WeaponModel, 1, (int)GetItemAppearance(sourceItem, ItemAppearanceType.WeaponModel, 1), true);
            copiedItem = CopyItemAndModify(copiedItem, ItemAppearanceType.WeaponColor, 1, (int)GetItemAppearance(sourceItem, ItemAppearanceType.WeaponColor, 1), true);

            copiedItem = CopyItemAndModify(copiedItem, ItemAppearanceType.WeaponModel, 2, (int)GetItemAppearance(sourceItem, ItemAppearanceType.WeaponModel, 2), true);
            copiedItem = CopyItemAndModify(copiedItem, ItemAppearanceType.WeaponColor, 2, (int)GetItemAppearance(sourceItem, ItemAppearanceType.WeaponColor, 2), true);

            SetName(copiedItem, GetName(sourceItem));
            SetDescription(copiedItem, GetDescription(sourceItem));
            //LocalVariableService.CopyVariables(oSource, copiedItem);

            var finalItem = (CopyItem(copiedItem, player, true));
            DeleteLocalString(finalItem, "TEMP_OUTFIT_UUID");

            if (copyPropsAndVars)
            {
                // strip all item props from new item
                for (var itemProperty = GetFirstItemProperty(finalItem); GetIsItemPropertyValid(itemProperty); itemProperty = GetNextItemProperty(finalItem))
                {
                    RemoveItemProperty(finalItem, itemProperty);
                }
                
                // add all item props from original item to new item
                for (var itemProperty = GetFirstItemProperty(sourceItem); GetIsItemPropertyValid(itemProperty); itemProperty = GetNextItemProperty(sourceItem))
                {
                    AddItemProperty(DurationType.Permanent, itemProperty, finalItem);
                }

                // finally, copy local vars
                Variable.CopyAll(sourceItem, finalItem);
            }

            DestroyObject(copiedItem);
            DestroyObject(destinationItem);

            for (var itemToDestroy = GetFirstItemInInventory(tempStorage); GetIsObjectValid(itemToDestroy); itemToDestroy = GetNextItemInInventory(tempStorage))
            {
                if (GetLocalString(itemToDestroy, "TEMP_OUTFIT_UUID") == playerId)
                {
                    DestroyObject(itemToDestroy);
                }
            }
            return finalItem;
        }
        public static void ToggleDualPistolMode(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            dbPlayer.IsUsingDualPistolMode = !dbPlayer.IsUsingDualPistolMode;

            DB.Set(playerId, dbPlayer);
            //Console.WriteLine("ToggleDualMode Changed To = " + dbPlayer.IsUsingDualPistolMode);

            var weapon = GetItemInSlot(InventorySlot.RightHand);
            
            if (Item.PistolBaseItemTypes.Contains(GetBaseItemType(weapon)) ||
                GetBaseItemType(weapon) == BaseItem.Sling)
            {
                ClearAllActions();

                // This isn't working for some reason:
                AssignCommand(player, () =>
                {
                    ActionUnequipItem(weapon);
                    ActionEquipItem(weapon, InventorySlot.RightHand);
                });
            }
        }
        private static void HandleOffhand(uint player, uint mainHandPistol)
        {
            var offHandPistol = CreateItemOnObject("offhandpistol", player);
            //if (NWNX.NWNXObject.CheckFit(player, (int)BaseItem.OffHandPistol) == 1)
            if (GetItemPossessor(offHandPistol) == player)
            {
                //Console.WriteLine("It fits!");

                offHandPistol = CopyWeaponAppearance(player, mainHandPistol, offHandPistol, false);
                AssignCommand(player, () =>
                {
                    ActionEquipItem(offHandPistol, InventorySlot.LeftHand);
                });
            }
            else
            {
                DelayCommand(0.5f, () =>
                {
                    AssignCommand(player, () =>
                    {
                        ActionUnequipItem(mainHandPistol);
                        DestroyObject(offHandPistol);
                    });
                });
            }
        }
        private static void ToggleDualModeWeapon(uint player)
        {
            var originalItem = GetPCItemLastEquipped();
            var mainHandPistol = originalItem;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (dbPlayer.IsUsingDualPistolMode)
            {
                mainHandPistol = CreateItemOnObject("dualpistolmain", player);
            }
            else
            {
                mainHandPistol = CreateItemOnObject("blaster_b", player);
            }

            mainHandPistol = CopyWeaponAppearance(player, originalItem, mainHandPistol, true);
            AssignCommand(player, () =>
            {
                ActionEquipItem(mainHandPistol, InventorySlot.RightHand);
            });

            if (dbPlayer.IsUsingDualPistolMode)
            {
                DelayCommand(0.2f, () => { HandleOffhand(player, mainHandPistol); });
            }

            DestroyObject(originalItem);
        }

        [NWNEventHandler("mod_equip")]
        public static void OnModuleEquipItem()
        {
            var player = GetPCItemLastEquippedBy();
            var item = GetPCItemLastEquipped();
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);           
            //Console.WriteLine("dbPlayer.IsUsingDualPistolMode Currently = " + dbPlayer.IsUsingDualPistolMode);

            // if equiping single wield pistol and not usign dual wield option then exit.
            if (Item.PistolBaseItemTypes.Contains(GetBaseItemType(item)) && !dbPlayer.IsUsingDualPistolMode) { return; }
            
            if (Item.PistolBaseItemTypes.Contains(GetBaseItemType(item)) && dbPlayer.IsUsingDualPistolMode)
            {
                ToggleDualModeWeapon(player);
            }
            else if (GetBaseItemType(item) == BaseItem.Sling && !dbPlayer.IsUsingDualPistolMode)
            {
                ToggleDualModeWeapon(player);
            }            
            else if (GetBaseItemType(item) == BaseItem.Sling && dbPlayer.IsUsingDualPistolMode)
            {
                DelayCommand(0.2f, () => { HandleOffhand(player, item); });
            }
        }

        [NWNEventHandler("mod_unequip")]
        public static void OnModuleUnequipItem()
        {            
            var player = GetPCItemLastUnequippedBy();            
            var item = GetPCItemLastUnequipped();

            if (!GetIsPC(player)) return;

            if (GetBaseItemType(item) == BaseItem.Sling)
            {
                var offHandPistol = GetItemInSlot(InventorySlot.LeftHand, player);
                //Console.WriteLine("Un Equiping Sling based item.");
                DestroyObject(offHandPistol);                
            }

            if (GetBaseItemType(item) == BaseItem.OffHandPistol)
            {
                var oMainHandItem = GetItemInSlot(InventorySlot.RightHand, player);
                AssignCommand(player, () =>
                {
                    ActionUnequipItem(oMainHandItem);
                });
                DestroyObject(item);
            }
        }

        [NWNEventHandler("mod_unacquire")]
        public static void OnModuleUnaquireItem()
        {
            var player = GetModuleItemLostBy();
            var item = GetModuleItemLost();

            if (!GetIsPC(player)) return;

            if (GetBaseItemType(item) == BaseItem.OffHandPistol)
            {                
                var mainHandItem = GetItemInSlot(InventorySlot.RightHand, player);
                if (GetBaseItemType(mainHandItem) == BaseItem.Sling)
                {
                    AssignCommand(player, () =>
                    {
                        ActionUnequipItem(mainHandItem);
                    });

                }
                DestroyObject(item);
            }
        }

    }
}
