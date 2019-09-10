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
            ByFeatID[entity.FeatID] = (PerkFeat)entity.Clone();
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
            return (PerkFeat)ByID[id].Clone();
        }

        public PerkFeat GetByPerkIDAndLevelUnlocked(int perkID, int levelUnlocked)
        {
            return GetEntityFromDictionary(perkID, levelUnlocked, ByPerkIDAndLevelUnlocked);
        }

        public PerkFeat GetByPerkIDAndLevelUnlockedOrDefault(int perkID, int levelUnlocked)
        {
            return GetEntityFromDictionaryOrDefault(perkID, levelUnlocked, ByPerkIDAndLevelUnlocked);
        }

        public PerkFeat GetByFeatID(int featID)
        {
            return (PerkFeat)ByFeatID[featID].Clone();
        }

        public PerkFeat GetByFeatIDOrDefault(int featID)
        {
            if (!ByFeatID.ContainsKey(featID))
            {
                return default;
            }

            return (PerkFeat)ByFeatID[featID].Clone();
        }

        public IEnumerable<PerkFeat> GetAllByIDs(IEnumerable<int> perkIDs)
        {
            var list = new List<PerkFeat>();
            foreach (var perkID in perkIDs)
            {
                if (ByFeatID.ContainsKey(perkID))
                {
                    list.Add((PerkFeat)ByFeatID[perkID].Clone());
                }
            }

            return list;
        }

        public IEnumerable<PerkFeat> GetAllByPerkID(int perkID)
        {
            if(!ByPerkIDAndLevelUnlocked.ContainsKey(perkID))
                return new List<PerkFeat>();

            var list = new List<PerkFeat>();
            foreach (var record in ByPerkIDAndLevelUnlocked[perkID].Values)
            {
                list.Add((PerkFeat)record.Clone());
            }

            return list;
        }
    }
}
