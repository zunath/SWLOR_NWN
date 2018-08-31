using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IKeyItemService
    {
        KeyItem GetKeyItemByID(int keyItemID);
        IEnumerable<PCKeyItem> GetPlayerKeyItemsByCategory(NWPlayer player, int categoryID);
        void GivePlayerKeyItem(NWPlayer oPC, int keyItemID);
        void OnModuleItemAcquired();
        bool PlayerHasKeyItem(NWObject oPC, int keyItemID);
        void RemovePlayerKeyItem(NWPlayer oPC, int keyItemID);
    }
}
