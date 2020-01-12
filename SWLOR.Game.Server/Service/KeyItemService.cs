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
using _ = SWLOR.Game.Server.NWScript._;


namespace SWLOR.Game.Server.Service
{
    public static class KeyItemService
    {
        private static readonly Dictionary<KeyItem, KeyItemAttribute> _keyItems = new Dictionary<KeyItem, KeyItemAttribute>();
        private static readonly Dictionary<KeyItemCategoryType, List<KeyItem>> _keyItemsByCategory = new Dictionary<KeyItemCategoryType, List<KeyItem>>();

        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleAcquireItem>(message => OnModuleItemAcquired());
        }

        public static void CacheData()
        {
            var keyItems = Enum.GetValues(typeof(KeyItem)).Cast<KeyItem>();
            foreach (var keyItem in keyItems)
            {
                var attr = keyItem.GetAttribute<KeyItem, KeyItemAttribute>();
                _keyItems[keyItem] = attr;

                if (!_keyItemsByCategory.ContainsKey(attr.Category))
                {
                    _keyItemsByCategory[attr.Category] = new List<KeyItem>();
                }

                _keyItemsByCategory[attr.Category].Add(keyItem);
            }
        }

        public static KeyItemAttribute GetKeyItem(KeyItem keyItem)
        {
            return _keyItems[keyItem];
        }

        public static bool PlayerHasKeyItem(NWObject oPC, KeyItem keyItemID)
        {
            var dbPlayer = DataService.Player.GetByID(oPC.GlobalID);
            return dbPlayer.AcquiredKeyItems.Contains(keyItemID);
        }

        public static bool PlayerHasAllKeyItems(NWObject oPC, params KeyItem[] keyItemIDs)
        {
            var dbPlayer = DataService.Player.GetByID(oPC.GlobalID);
            foreach (var keyItem in keyItemIDs)
            {
                if (!dbPlayer.AcquiredKeyItems.Contains(keyItem)) return false;
            }

            return true;
        }

        public static bool PlayerHasAnyKeyItem(NWObject oPC, params KeyItem[] keyItemIDs)
        {
            var dbPlayer = DataService.Player.GetByID(oPC.GlobalID);
            foreach (var keyItem in keyItemIDs)
            {
                if (dbPlayer.AcquiredKeyItems.Contains(keyItem)) return true;
            }

            return false;
        }


        public static void GivePlayerKeyItem(NWPlayer oPC, KeyItem keyItemID)
        {
            if (keyItemID == KeyItem.Invalid) return;

            if (!PlayerHasKeyItem(oPC, keyItemID))
            {
                var dbPlayer = DataService.Player.GetByID(oPC.GlobalID);
                dbPlayer.AcquiredKeyItems.Add(keyItemID);

                DataService.Set(dbPlayer);

                var keyItem = GetKeyItem(keyItemID); 
                oPC.SendMessage("You acquired the key item '" + keyItem.Name + "'.");
            }
        }

        public static void RemovePlayerKeyItem(NWPlayer oPC, KeyItem keyItemID)
        {
            if (keyItemID == KeyItem.Invalid) return;

            if (PlayerHasKeyItem(oPC, keyItemID))
            {
                var dbPlayer = DataService.Player.GetByID(oPC.GlobalID);
                dbPlayer.AcquiredKeyItems.Remove(keyItemID);
                DataService.Set(dbPlayer);
            }
        }

        public static IEnumerable<KeyItem> GetPlayerKeyItemsByCategory(NWPlayer player, KeyItemCategoryType categoryID)
        {
            return _keyItemsByCategory[categoryID];
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
