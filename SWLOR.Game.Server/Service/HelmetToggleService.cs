using System;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.ValueObject;
using BaseItemType = SWLOR.Game.Server.NWScript.Enumerations.BaseItemType;

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
            if (player.GetLocalBoolean("IS_CUSTOMIZING_ITEM") == true) return; // Don't run heavy code when customizing equipment.

            if (!player.IsPlayer || !player.IsInitializedAsPlayer) return;

            NWItem item = (_.GetPCItemLastEquipped());
            if (item.BaseItemType != BaseItemType.Helmet) return;

            Player pc = DataService.Player.GetByID(player.GlobalID);
            _.SetHiddenWhenEquipped(item.Object, !pc.DisplayHelmet);
        }

        private static void OnModuleUnequipItem()
        {
            NWPlayer player = (_.GetPCItemLastUnequippedBy());

            if (player.GetLocalBoolean("IS_CUSTOMIZING_ITEM") == true) return; // Don't run heavy code when customizing equipment.
            if (!player.IsPlayer) return;

            NWItem item = (_.GetPCItemLastUnequipped());
            if (item.BaseItemType != BaseItemType.Helmet) return;

            Player pc = DataService.Player.GetByID(player.GlobalID);
            _.SetHiddenWhenEquipped(item.Object, !pc.DisplayHelmet);
        
        }

        public static void ToggleHelmetDisplay(NWPlayer player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));

            if (!player.IsPlayer) return;

            Player pc = DataService.Player.GetByID(player.GlobalID);
            pc.DisplayHelmet = !pc.DisplayHelmet;
            DataService.Set(pc);
            
            _.FloatingTextStringOnCreature(
                pc.DisplayHelmet ? "Now showing equipped helmet." : "Now hiding equipped helmet.", 
                player.Object,
                false);

            NWItem helmet = (_.GetItemInSlot(InventorySlot.Head, player.Object));
            if (helmet.IsValid)
            {
                _.SetHiddenWhenEquipped(helmet.Object, !pc.DisplayHelmet);
            }

        }
    }
}
