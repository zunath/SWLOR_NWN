using System;
using System.Collections.Generic;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;


namespace SWLOR.Game.Server.Service
{
    public static class KeyItemService
    {
        private static Dictionary<KeyItem, KeyItemAttribute> _keyItems = new Dictionary<KeyItem, KeyItemAttribute>();

        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleAcquireItem>(message => OnModuleItemAcquired());
            MessageHub.Instance.Subscribe<OnModuleLoad>(message => OnModuleLoad());
        }

        private static void OnModuleLoad()
        {
            var keyItems = Enum.GetValues(typeof(KeyItem)).Cast<KeyItem>();
            foreach (var keyItem in keyItems)
            {
                _keyItems[keyItem] = keyItem.GetAttribute<KeyItem, KeyItemAttribute>();
            }
        }

        public static KeyItemAttribute GetKeyItem(KeyItem keyItem)
        {
            return _keyItems[keyItem];
        }

        public static bool PlayerHasKeyItem(NWObject oPC, KeyItem keyItemID)
        {
            var entity = DataService.PCKeyItem.GetByPlayerAndKeyItemIDOrDefault(oPC.GlobalID, keyItemID); 
            return entity != null;
        }

        public static bool PlayerHasAllKeyItems(NWObject oPC, params KeyItem[] keyItemIDs)
        {
            var result = DataService.PCKeyItem.GetAllByPlayerIDAndKeyItemIDs(oPC.GlobalID, keyItemIDs);
            return result.Count() == keyItemIDs.Length;
        }

        public static bool PlayerHasAnyKeyItem(NWObject oPC, params KeyItem[] keyItemIDs)
        {
            var pcKeyItems = DataService.PCKeyItem.GetAllByPlayerID(oPC.GlobalID);
            return pcKeyItems.Any(x => x.PlayerID == oPC.GlobalID && keyItemIDs.Contains(x.KeyItemID));
        }


        public static void GivePlayerKeyItem(NWPlayer oPC, KeyItem keyItemID)
        {
            if (keyItemID == KeyItem.Invalid) return;

            if (!PlayerHasKeyItem(oPC, keyItemID))
            {
                PCKeyItem entity = new PCKeyItem
                {
                    PlayerID = oPC.GlobalID,
                    KeyItemID = keyItemID,
                    AcquiredDate = DateTime.UtcNow
                };
                DataService.SubmitDataChange(entity, DatabaseActionType.Insert);

                var keyItem = GetKeyItem(keyItemID); 
                oPC.SendMessage("You acquired the key item '" + keyItem.Name + "'.");
            }
        }

        public static void RemovePlayerKeyItem(NWPlayer oPC, KeyItem keyItemID)
        {
            if (keyItemID == KeyItem.Invalid) return;

            if (PlayerHasKeyItem(oPC, keyItemID))
            {
                PCKeyItem entity = DataService.PCKeyItem.GetByPlayerAndKeyItemID(oPC.GlobalID, keyItemID);
                DataService.SubmitDataChange(entity, DatabaseActionType.Delete);
            }
        }

        public static IEnumerable<PCKeyItem> GetPlayerKeyItemsByCategory(NWPlayer player, KeyItemCategoryType categoryID)
        {
            return DataService.PCKeyItem.GetAllByPlayerID(player.GlobalID).Where(x =>
            {
                var keyItem = GetKeyItem(x.KeyItemID);
                return keyItem.Category == categoryID;
            }).ToList();
        }

        private static void OnModuleItemAcquired()
        {
            NWPlayer oPC = (_.GetModuleItemAcquiredBy());

            if (!oPC.IsPlayer) return;

            NWItem oItem = (_.GetModuleItemAcquired());
            var keyItemID = (KeyItem)oItem.GetLocalInt("KEY_ITEM_ID");

            if (keyItemID == KeyItem.Invalid) return;

            GivePlayerKeyItem(oPC, keyItemID);
            oItem.Destroy();
        }
    }
}
