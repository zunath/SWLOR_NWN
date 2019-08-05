using System;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.ValueObject;

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
            NWPlayer player = (_.GetPCItemLastEquippedBy());
            if (player.GetLocalInt("IS_CUSTOMIZING_ITEM") == _.TRUE) return; // Don't run heavy code when customizing equipment.

            if (!player.IsPlayer || !player.IsInitializedAsPlayer) return;

            NWItem item = (_.GetPCItemLastEquipped());
            if (item.BaseItemType != _.BASE_ITEM_HELMET) return;

            Player pc = DataService.Player.GetByID(player.GlobalID);
            _.SetHiddenWhenEquipped(item.Object, !pc.DisplayHelmet == false ? 0 : 1);
        
        }

        private static void OnModuleUnequipItem()
        {
            NWPlayer player = (_.GetPCItemLastUnequippedBy());

            if (player.GetLocalInt("IS_CUSTOMIZING_ITEM") == _.TRUE) return; // Don't run heavy code when customizing equipment.
            if (!player.IsPlayer) return;

            NWItem item = (_.GetPCItemLastUnequipped());
            if (item.BaseItemType != _.BASE_ITEM_HELMET) return;

            Player pc = DataService.Player.GetByID(player.GlobalID);
            _.SetHiddenWhenEquipped(item.Object, !pc.DisplayHelmet == false ? 0 : 1);
        
        }

        public static void ToggleHelmetDisplay(NWPlayer player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));

            if (!player.IsPlayer) return;

            Player pc = DataService.Player.GetByID(player.GlobalID);
            pc.DisplayHelmet = !pc.DisplayHelmet;
            DataService.SubmitDataChange(pc, DatabaseActionType.Update);
            
            _.FloatingTextStringOnCreature(
                pc.DisplayHelmet ? "Now showing equipped helmet." : "Now hiding equipped helmet.", 
                player.Object,
                _.FALSE);

            NWItem helmet = (_.GetItemInSlot(_.INVENTORY_SLOT_HEAD, player.Object));
            if (helmet.IsValid)
            {
                _.SetHiddenWhenEquipped(helmet.Object, !pc.DisplayHelmet == false ? 0 : 1);
            }

        }
    }
}
