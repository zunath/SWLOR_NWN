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
            return (PCPerk)ByID[id].Clone();
        }

        public PCPerk GetByPlayerAndPerkID(Guid playerID, int perkID)
        {
            return GetEntityFromDictionary(playerID, perkID, ByPlayerAndPerkID);
        }

        public PCPerk GetByPlayerAndPerkIDOrDefault(Guid playerID, int perkID)
        {
            return GetEntityFromDictionaryOrDefault(playerID, perkID, ByPlayerAndPerkID);
        }

        public IEnumerable<PCPerk> GetAllByPlayerID(Guid playerID)
        {
            if (!ByPlayerAndPerkID.ContainsKey(playerID))
            {
                return new List<PCPerk>();
            }

            var list = new List<PCPerk>();
            foreach (var record in ByPlayerAndPerkID[playerID].Values)
            {
                list.Add((PCPerk)record.Clone());
            }

            return list;
        }
    }
}
