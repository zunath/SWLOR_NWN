using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service
{
    public static class DualPistolService
    {

        public static void SubscribeEvents()
        {
            // Module Events
            MessageHub.Instance.Subscribe<OnModuleEquipItem>(message => OnModuleEquipItem());
            MessageHub.Instance.Subscribe<OnModuleUnequipItem>(message => OnModuleUnequipItem());
            MessageHub.Instance.Subscribe<OnModuleUnacquireItem>(message => OnModuleUnaquireItem());
            MessageHub.Instance.Subscribe<OnModuleAcquireItem>(message => OnModuleAquireItem());
        }
        private static NWItem CopyWeaponAppearance(NWPlayer oPC, NWItem oSource, NWItem oDest, bool copyPropsAndVars)
        {
            NWPlaceable oTempStorage = (GetObjectByTag("OUTFIT_BARREL"));
            oSource.SetLocalString("TEMP_OUTFIT_UUID", oPC.GlobalID.ToString());
            
            var oCopy = CopyItem(oDest.Object, oTempStorage.Object, true);
            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.WeaponModel, 0, (int)GetItemAppearance(oSource, ItemAppearanceType.WeaponModel, 0), true);
            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.WeaponColor, 0, (int)GetItemAppearance(oSource, ItemAppearanceType.WeaponColor, 0), true);

            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.WeaponModel, 1, (int)GetItemAppearance(oSource, ItemAppearanceType.WeaponModel, 1), true);
            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.WeaponColor, 1, (int)GetItemAppearance(oSource, ItemAppearanceType.WeaponColor, 1), true);

            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.WeaponModel, 2, (int)GetItemAppearance(oSource, ItemAppearanceType.WeaponModel, 2), true);
            oCopy = CopyItemAndModify(oCopy, ItemAppearanceType.WeaponColor, 2, (int)GetItemAppearance(oSource, ItemAppearanceType.WeaponColor, 2), true);

            SetName(oCopy, GetName(oSource));
            SetDescription(oCopy, GetDescription(oSource));
            //LocalVariableService.CopyVariables(oSource, oCopy);

            NWItem oFinal = (CopyItem(oCopy, oPC.Object, true));
            oFinal.DeleteLocalString("TEMP_OUTFIT_UUID");

            if (copyPropsAndVars)
            {
                // strip all item props from new item
                foreach (var itemProp in oFinal.ItemProperties)
                {
                    RemoveItemProperty(oFinal, itemProp);
                }
                // add all item props from original item to new item
                foreach (var itemProp in oSource.ItemProperties)
                {
                    AddItemProperty(DurationType.Permanent, itemProp, oFinal);
                }
                // finally, copy local vars
                LocalVariableService.CopyVariables(oSource, oFinal);
            }

            DestroyObject(oCopy);
            oDest.Destroy();

            foreach (var item in oTempStorage.InventoryItems)
            {
                if (item.GetLocalString("TEMP_OUTFIT_UUID") == oPC.GlobalID.ToString())
                {
                    item.Destroy();
                }
            }
            return oFinal;
        }
        public static void ToggleDualPistolMode(NWPlayer oPC)
        {
            var pc = DataService.Player.GetByID(oPC.GlobalID);
            pc.ModeDualPistol = !pc.ModeDualPistol;
            DataService.SubmitDataChange(pc, DatabaseActionType.Update);
            //Console.WriteLine("ToggleDualMode Changed To = " + pc.ModeDualPistol);

            NWItem oItem = GetItemInSlot(InventorySlot.RightHand);

            if (oItem.BaseItemType == BaseItem.ShortBow ||
                oItem.BaseItemType == BaseItem.Sling)
            {
                oPC.ClearAllActions();
                
                // This isn't working for some reason:
                oPC.AssignCommand(() =>
                {
                    ActionUnequipItem(oItem);
                    ActionEquipItem(oItem, InventorySlot.RightHand);
                });
            }
        }
        private static void HandleOffhand(NWPlayer oPC, NWItem oMainHandPistol)
        {
            NWItem oOffHandPistol = CreateItemOnObject("offhandpistol", oPC);
            //if (NWNX.NWNXObject.CheckFit(oPC, (int)BaseItem.OffHandPistol) == 1)
            if (oOffHandPistol.Possessor == oPC)
            {
                //Console.WriteLine("It fits!");

                oOffHandPistol = CopyWeaponAppearance(oPC, oMainHandPistol, oOffHandPistol, false);
                oPC.AssignCommand(() =>
                {
                    ActionEquipItem(oOffHandPistol, InventorySlot.LeftHand);
                });
            }
            else
            {
                //Console.WriteLine("It doesn't fit :(");
                oPC.DelayAssignCommand(() =>
                {
                    ActionUnequipItem(oMainHandPistol);
                    DestroyObject(oOffHandPistol);
                }, 0.5f);
            }
        }
        private static void ToggleDualModeWeapon(NWPlayer oPC)
        {
            NWItem oOriginalItem = NWScript.GetPCItemLastEquipped();
            NWItem oMainHandPistol;

            var pc = DataService.Player.GetByID(oPC.GlobalID);

            if (pc.ModeDualPistol)
            {
                oMainHandPistol = CreateItemOnObject("dualpistolmain", oPC);
            }
            else
            {
                oMainHandPistol = CreateItemOnObject("blaster_b", oPC);
            }

            oMainHandPistol = CopyWeaponAppearance(oPC, oOriginalItem, oMainHandPistol, true);
            oPC.AssignCommand(() =>
            {
                ActionEquipItem(oMainHandPistol, InventorySlot.RightHand);
            });

            if (pc.ModeDualPistol)
            {
                NWScript.DelayCommand(0.2f, () => { HandleOffhand(oPC, oMainHandPistol); });
            }

            oOriginalItem.Destroy();
        }            

        private static void OnModuleEquipItem()
        {
            NWPlayer oPC = NWScript.GetPCItemLastEquippedBy();
            NWItem oItem = NWScript.GetPCItemLastEquipped();

            if (GetLocalBool(oPC, "IS_CUSTOMIZING_ITEM")) return; // Don't run heavy code when customizing equipment.
            if (!oPC.IsPlayer || !oPC.IsInitializedAsPlayer) return;

            if (GetLocalBool(oPC, "LOGGED_IN_ONCE") == false) return;

            var pc = DataService.Player.GetByID(oPC.GlobalID);
            //Console.WriteLine("pc.ModeDualPistol Currently = " + pc.ModeDualPistol);

            // if equiping single wield pistol and not usign dual wield option then exit.
            if (oItem.BaseItemType == BaseItem.ShortBow && !pc.ModeDualPistol) { return; }
            
            if (oItem.BaseItemType == BaseItem.ShortBow && pc.ModeDualPistol)
            {
                ToggleDualModeWeapon(oPC);
            }
            else if (oItem.BaseItemType == BaseItem.Sling && !pc.ModeDualPistol)
            {
                ToggleDualModeWeapon(oPC);
            }            
            else if (oItem.BaseItemType == BaseItem.Sling && pc.ModeDualPistol)
            {
                NWScript.DelayCommand(0.2f, () => { HandleOffhand(oPC, oItem); });
            }
        }

        private static void OnModuleUnequipItem()
        {            
            NWPlayer oPC = NWScript.GetPCItemLastUnequippedBy();            
            NWItem oItem = NWScript.GetPCItemLastUnequipped();

            if (GetLocalBool(oPC, "IS_CUSTOMIZING_ITEM")) return; // Don't run heavy code when customizing equipment.
            if (!oPC.IsPlayer) return;

            if (oItem.BaseItemType == BaseItem.Sling)
            {
                NWItem oOffHandPistol = GetItemInSlot(InventorySlot.LeftHand, oPC);
                //Console.WriteLine("Un Equiping Sling based item.");
                DestroyObject(oOffHandPistol);                
            }

            if (oItem.BaseItemType == BaseItem.OffHandPistol)
            {
                NWItem oMainHandItem = GetItemInSlot(InventorySlot.RightHand, oPC);
                oPC.AssignCommand(() =>
                {
                    ActionUnequipItem(oMainHandItem);
                });
                DestroyObject(oItem);
            }
        }
        private static void OnModuleUnaquireItem()
        {
            NWPlayer oPC = NWScript.GetModuleItemLostBy();
            NWItem oItem = NWScript.GetModuleItemLost();

            if (GetLocalBool(oPC, "IS_CUSTOMIZING_ITEM")) return; // Don't run heavy code when customizing equipment.
            if (!oPC.IsPlayer) return;

            if (oItem.BaseItemType == BaseItem.OffHandPistol)
            {                
                NWItem oMainHandItem = GetItemInSlot(InventorySlot.RightHand, oPC);
                if (oMainHandItem.BaseItemType == BaseItem.Sling)
                {
                    oPC.AssignCommand(() =>
                    {
                        ActionUnequipItem(oMainHandItem);
                    });

                }
                DestroyObject(oItem);
            }
        }
        private static void OnModuleAquireItem()
        {
        }
    }
}
