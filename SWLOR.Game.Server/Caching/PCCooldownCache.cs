using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCCooldownCache: CacheBase<PCCooldown>
    {
        private Dictionary<Guid, Dictionary<int, PCCooldown>> ByPlayerAndCooldownCategoryID { get; } = new Dictionary<Guid, Dictionary<int, PCCooldown>>();

        protected override void OnCacheObjectSet(PCCooldown entity)
        {
            SetEntityIntoDictionary(entity.PlayerID, entity.CooldownCategoryID, entity, ByPlayerAndCooldownCategoryID);
        }

        protected override void OnCacheObjectRemoved(PCCooldown entity)
        {
            RemoveEntityFromDictionary(entity.PlayerID, entity.CooldownCategoryID, ByPlayerAndCooldownCategoryID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCCooldown GetByID(Guid id)
        {
            return (PCCooldown)ByID[id].Clone();
        }

        public PCCooldown GetByPlayerAndCooldownCategoryIDOrDefault(Guid playerID, int cooldownCategoryID)
        {
            return GetEntityFromDictionaryOrDefault(playerID, cooldownCategoryID, ByPlayerAndCooldownCategoryID);
        }

        public PCCooldown GetByPlayerAndCooldownCategoryID(Guid playerID, int cooldownCategoryID)
        {
            return GetEntityFromDictionary(playerID, cooldownCategoryID, ByPlayerAndCooldownCategoryID);
        }

        public IEnumerable<PCCooldown> GetAllByPlayerID(Guid playerID)
        {
            if (!ByPlayerAndCooldownCategoryID.ContainsKey(playerID))
            {
                return new List<PCCooldown>();
            }

            var list = new List<PCCooldown>();
            foreach (var record in ByPlayerAndCooldownCategoryID[playerID].Values)
            {
                list.Add((PCCooldown)record.Clone());
            }

            return list;
        }
    }
}
