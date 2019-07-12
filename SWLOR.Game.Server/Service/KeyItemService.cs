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
            var entity = DataService.GetAll<PCKeyItem>().FirstOrDefault(x => x.PlayerID == oPC.GlobalID && x.KeyItemID == keyItemID);
            return entity != null;
        }

        public static bool PlayerHasAllKeyItems(NWObject oPC, params int[] keyItemIDs)
        {
            var result = DataService.Where<PCKeyItem>(x => x.PlayerID == oPC.GlobalID && keyItemIDs.Contains(x.KeyItemID)).ToList();
            return result.Count() == keyItemIDs.Length;
        }

        public static bool PlayerHasAnyKeyItem(NWObject oPC, params int[] keyItemIDs)
        {
            return DataService.GetAll<PCKeyItem>().Any(x => x.PlayerID == oPC.GlobalID && keyItemIDs.Contains(x.KeyItemID));
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
                
                KeyItem keyItem = DataService.Single<KeyItem>(x => x.ID == keyItemID);
                oPC.SendMessage("You acquired the key item '" + keyItem.Name + "'.");
            }
        }

        public static void RemovePlayerKeyItem(NWPlayer oPC, int keyItemID)
        {
            if (PlayerHasKeyItem(oPC, keyItemID))
            {

                PCKeyItem entity = DataService.Single<PCKeyItem>(x => x.PlayerID == oPC.GlobalID && x.KeyItemID == keyItemID);
                DataService.SubmitDataChange(entity, DatabaseActionType.Delete);
            }
        }

        public static IEnumerable<PCKeyItem> GetPlayerKeyItemsByCategory(NWPlayer player, int categoryID)
        {
            return DataService.Where<PCKeyItem>(x =>
            {
                var keyItem = DataService.Get<KeyItem>(x.KeyItemID);
                return x.PlayerID == player.GlobalID && keyItem.KeyItemCategoryID == categoryID;
            }).ToList();
        }

        public static KeyItem GetKeyItemByID(int keyItemID)
        {
            return DataService.Single<KeyItem>(x => x.ID == keyItemID);
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
