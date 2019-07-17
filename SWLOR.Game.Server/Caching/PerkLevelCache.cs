using System;
using System.Collections.Generic;
using System.Linq;
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
            return (PerkLevel)ByID[id].Clone();
        }

        public PerkLevel GetByPerkIDAndLevel(int perkID, int level)
        {
            return GetEntityFromDictionary(perkID, level, ByPerkIDAndLevel);
        }

        public IEnumerable<PerkLevel> GetAllByPerkID(int perkID)
        {
            var list = new List<PerkLevel>();
            if (!ByPerkIDAndLevel.ContainsKey(perkID))
                return list;

            foreach (var record in ByPerkIDAndLevel[perkID].Values)
            {
                list.Add( (PerkLevel)record.Clone());
            }

            return list;
        }

        public IEnumerable<PerkLevel> GetAllAtOrBelowPerkIDAndLevel(int perkID, int level)
        {
            if(!ByPerkIDAndLevel.ContainsKey(perkID))
                return new List<PerkLevel>();

            var list = new List<PerkLevel>();
            foreach (var record in ByPerkIDAndLevel[perkID].Values.Where(x => x.Level <= level))
            {
                list.Add((PerkLevel)record.Clone());
            }
            return list;
        }
    }
}
