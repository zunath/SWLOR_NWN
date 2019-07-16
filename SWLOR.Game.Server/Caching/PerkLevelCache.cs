using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PerkLevelCache: CacheBase<PerkLevel>
    {
        private Dictionary<int, Dictionary<int, PerkLevel>> ByPerkIDAndLevel { get; } = new Dictionary<int, Dictionary<int, PerkLevel>>();

        protected override void OnCacheObjectSet(PerkLevel entity)
        {
            SetEntityIntoDictionary(entity.PerkID, entity.Level, entity, ByPerkIDAndLevel);
        }

        protected override void OnCacheObjectRemoved(PerkLevel entity)
        {
            RemoveEntityFromDictionary(entity.PerkID, entity.Level, ByPerkIDAndLevel);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PerkLevel GetByID(int id)
        {
            return ByID[id];
        }

        public PerkLevel GetByPerkIDAndLevel(int perkID, int level)
        {
            return GetEntityFromDictionary(perkID, level, ByPerkIDAndLevel);
        }

        public IEnumerable<PerkLevel> GetAllByPerkID(int perkID)
        {
            return ByPerkIDAndLevel[perkID].Values;
        }
    }
}
