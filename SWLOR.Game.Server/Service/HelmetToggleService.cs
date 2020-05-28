using System;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.NWN.Enum;
using SWLOR.Game.Server.NWN.Enum.Item;
using SWLOR.Game.Server.ValueObject;
using static SWLOR.Game.Server.NWN._;

namespace SWLOR.Game.Server.Service
{
    public static class HelmetToggleService
    {
        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleEquipItem>(message => OnModuleEquipItem());
            MessageHub.Instance.Subscribe<OnModuleUnequipItem>(message => OnModuleUnequipItem());
        }
        
        private static void OnModuleEquipItem()
        {
            NWPlayer player = (GetPCItemLastEquippedBy());
            if (GetLocalBool(player, "IS_CUSTOMIZING_ITEM")) return; // Don't run heavy code when customizing equipment.

            if (!player.IsPlayer || !player.IsInitializedAsPlayer) return;

            NWItem item = (GetPCItemLastEquipped());
            if (item.BaseItemType != BaseItem.Helmet) return;

            Player pc = DataService.Player.GetByID(player.GlobalID);
            SetHiddenWhenEquipped(item.Object, !pc.DisplayHelmet == false ? 0 : 1);
        
        }

        private static void OnModuleUnequipItem()
        {
            NWPlayer player = (GetPCItemLastUnequippedBy());

            if (GetLocalBool(player, "IS_CUSTOMIZING_ITEM") == true) return; // Don't run heavy code when customizing equipment.
            if (!player.IsPlayer) return;

            NWItem item = (GetPCItemLastUnequipped());
            if (item.BaseItemType != BaseItem.Helmet) return;

            Player pc = DataService.Player.GetByID(player.GlobalID);
            SetHiddenWhenEquipped(item.Object, !pc.DisplayHelmet == false ? 0 : 1);
        
        }

        public static void ToggleHelmetDisplay(NWPlayer player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));

            if (!player.IsPlayer) return;

            Player pc = DataService.Player.GetByID(player.GlobalID);
            pc.DisplayHelmet = !pc.DisplayHelmet;
            DataService.SubmitDataChange(pc, DatabaseActionType.Update);
            
            FloatingTextStringOnCreature(
                pc.DisplayHelmet ? "Now showing equipped helmet." : "Now hiding equipped helmet.", 
                player.Object,
                false);

            NWItem helmet = (GetItemInSlot(InventorySlot.Head, player.Object));
            if (helmet.IsValid)
            {
                SetHiddenWhenEquipped(helmet.Object, !pc.DisplayHelmet == false ? 0 : 1);
            }

        }
    }
}
