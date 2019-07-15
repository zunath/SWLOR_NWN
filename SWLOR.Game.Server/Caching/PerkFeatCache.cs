using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PerkFeatCache: CacheBase<PerkFeat>
    {
        private Dictionary<int, Dictionary<int, PerkFeat>> ByPerkIDAndLevelUnlocked { get; } = new Dictionary<int, Dictionary<int, PerkFeat>>();
        private Dictionary<int, PerkFeat> ByFeatID { get; } = new Dictionary<int, PerkFeat>();

        protected override void OnCacheObjectSet(PerkFeat entity)
        {
            SetEntityIntoDictionary(entity.PerkID, entity.PerkLevelUnlocked, entity, ByPerkIDAndLevelUnlocked);
            ByFeatID[entity.FeatID] = entity;
        }

        protected override void OnCacheObjectRemoved(PerkFeat entity)
        {
            RemoveEntityFromDictionary(entity.PerkID, entity.PerkLevelUnlocked, ByPerkIDAndLevelUnlocked);
            ByFeatID.Remove(entity.FeatID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PerkFeat GetByID(int id)
        {
            return ByID[id];
        }

        public PerkFeat GetByPerkIDAndLevelUnlocked(int perkID, int levelUnlocked)
        {
            return GetEntityFromDictionary(perkID, levelUnlocked, ByPerkIDAndLevelUnlocked);
        }

        public PerkFeat GetByFeatID(int featID)
        {
            return ByFeatID[featID];
        }

        public PerkFeat GetByFeatIDOrDefault(int featID)
        {
            if (!ByFeatID.ContainsKey(featID))
            {
                return default;
            }

            return ByFeatID[featID];
        }
    }
}
