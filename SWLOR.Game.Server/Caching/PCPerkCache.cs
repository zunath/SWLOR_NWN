using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCPerkCache: CacheBase<PCPerk>
    {
        private Dictionary<Guid, Dictionary<int, PCPerk>> ByPlayerAndPerkID { get; } = new Dictionary<Guid, Dictionary<int, PCPerk>>();

        protected override void OnCacheObjectSet(PCPerk entity)
        {
            SetEntityIntoDictionary(entity.PlayerID, entity.PerkID, entity, ByPlayerAndPerkID);
        }

        protected override void OnCacheObjectRemoved(PCPerk entity)
        {
            RemoveEntityFromDictionary(entity.PlayerID, entity.PerkID, ByPlayerAndPerkID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCPerk GetByID(Guid id)
        {
            return ByID[id];
        }

        public PCPerk GetByPlayerAndPerkID(Guid playerID, int perkID)
        {
            return GetEntityFromDictionary(playerID, perkID, ByPlayerAndPerkID);
        }

        public IEnumerable<PCPerk> GetAllByPlayerID(Guid playerID)
        {
            if (!ByPlayerAndPerkID.ContainsKey(playerID))
            {
                ByPlayerAndPerkID[playerID] = new Dictionary<int, PCPerk>();
            }

            return ByPlayerAndPerkID[playerID].Values;
        }
    }
}
