using System;
using System.Collections.Generic;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Service
{
    public class KeyItemService : IKeyItemService
    {
        private readonly IDataService _data;
        private readonly INWScript _;

        public KeyItemService(IDataService data, INWScript script)
        {
            _data = data;
            _ = script;
        }

        public bool PlayerHasKeyItem(NWObject oPC, int keyItemID)
        {
            var entity = _data.PCKeyItems.FirstOrDefault(x => x.PlayerID == oPC.GlobalID && x.KeyItemID == keyItemID);
            return entity != null;
        }

        public bool PlayerHasAllKeyItems(NWObject oPC, params int[] keyItemIDs)
        {
            var result = _data.PCKeyItems.Where(x => x.PlayerID == oPC.GlobalID && keyItemIDs.Contains(x.KeyItemID)).ToList();
            return result.Count() == keyItemIDs.Length;
        }

        public bool PlayerHasAnyKeyItem(NWObject oPC, params int[] keyItemIDs)
        {
            return _data.PCKeyItems.Any(x => x.PlayerID == oPC.GlobalID && keyItemIDs.Contains(x.KeyItemID));
        }


        public void GivePlayerKeyItem(NWPlayer oPC, int keyItemID)
        {
            if (!PlayerHasKeyItem(oPC, keyItemID))
            {
                PCKeyItem entity = new PCKeyItem
                {
                    PlayerID = oPC.GlobalID,
                    KeyItemID = keyItemID,
                    AcquiredDate = DateTime.UtcNow
                };
                _data.PCKeyItems.Add(entity);
                _data.SaveChanges();

                KeyItem keyItem = _data.KeyItems.Single(x => x.KeyItemID == keyItemID);
                oPC.SendMessage("You acquired the key item '" + keyItem.Name + "'.");
            }
        }

        public void RemovePlayerKeyItem(NWPlayer oPC, int keyItemID)
        {
            if (PlayerHasKeyItem(oPC, keyItemID))
            {

                PCKeyItem entity = _data.PCKeyItems.Single(x => x.PlayerID == oPC.GlobalID && x.KeyItemID == keyItemID);
                _data.PCKeyItems.Remove(entity);
                _data.SaveChanges();
            }
        }

        public IEnumerable<PCKeyItem> GetPlayerKeyItemsByCategory(NWPlayer player, int categoryID)
        {
            return _data.PCKeyItems.Where(x => x.PlayerID == player.GlobalID && x.KeyItem.KeyItemCategoryID == categoryID).ToList();
        }

        public KeyItem GetKeyItemByID(int keyItemID)
        {
            return _data.KeyItems.Single(x => x.KeyItemID == keyItemID);
        }

        public void OnModuleItemAcquired()
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
