using System;
using System.Collections.Generic;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;


namespace SWLOR.Game.Server.Service
{
    public static class KeyItemService
    {
        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleAcquireItem>(message => OnModuleItemAcquired());
        }

        public static bool PlayerHasKeyItem(NWObject oPC, int keyItemID)
        {
            var entity = DataService.PCKeyItem.GetByPlayerAndKeyItemIDOrDefault(oPC.GlobalID, keyItemID); 
            return entity != null;
        }

        public static bool PlayerHasAllKeyItems(NWObject oPC, params int[] keyItemIDs)
        {
            var result = DataService.PCKeyItem.GetAllByPlayerIDAndKeyItemIDs(oPC.GlobalID, keyItemIDs);
            return result.Count() == keyItemIDs.Length;
        }

        public static bool PlayerHasAnyKeyItem(NWObject oPC, params int[] keyItemIDs)
        {
            var pcKeyItems = DataService.PCKeyItem.GetAllByPlayerID(oPC.GlobalID);
            return pcKeyItems.Any(x => x.PlayerID == oPC.GlobalID && keyItemIDs.Contains(x.KeyItemID));
        }


        public static void GivePlayerKeyItem(NWPlayer oPC, int keyItemID)
        {
            if (!PlayerHasKeyItem(oPC, keyItemID))
            {
                PCKeyItem entity = new PCKeyItem
                {
                    PlayerID = oPC.GlobalID,
                    KeyItemID = keyItemID,
                    AcquiredDate = DateTime.UtcNow
                };
                DataService.SubmitDataChange(entity, DatabaseActionType.Insert);
                
                KeyItem keyItem = DataService.KeyItem.GetByID(keyItemID);
                oPC.SendMessage("You acquired the key item '" + keyItem.Name + "'.");
            }
        }

        public static void RemovePlayerKeyItem(NWPlayer oPC, int keyItemID)
        {
            if (PlayerHasKeyItem(oPC, keyItemID))
            {

                PCKeyItem entity = DataService.PCKeyItem.GetByPlayerAndKeyItemID(oPC.GlobalID, keyItemID);
                DataService.SubmitDataChange(entity, DatabaseActionType.Delete);
            }
        }

        public static IEnumerable<PCKeyItem> GetPlayerKeyItemsByCategory(NWPlayer player, int categoryID)
        {
            return DataService.PCKeyItem.GetAllByPlayerID(player.GlobalID).Where(x =>
            {
                var keyItem = DataService.KeyItem.GetByID(x.KeyItemID);
                return keyItem.KeyItemCategoryID == categoryID;
            }).ToList();
        }

        public static KeyItem GetKeyItemByID(int keyItemID)
        {
            return DataService.KeyItem.GetByID(keyItemID);
        }

        private static void OnModuleItemAcquired()
        {
            NWPlayer oPC = (_.GetModuleItemAcquiredBy());

            if (!oPC.IsPlayer) return;

            NWItem oItem = (_.GetModuleItemAcquired());
            int keyItemID = oItem.GetLocalInt("KEY_ITEM_ID");

            if (keyItemID <= 0) return;

            GivePlayerKeyItem(oPC, keyItemID);
            oItem.Destroy();
        }
    }
}
