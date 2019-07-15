using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class AreaCache: CacheBase<Area>
    {
        private Dictionary<string, Area> ByResref { get; } = new Dictionary<string, Area>();

        protected override void OnCacheObjectSet(Area entity)
        {
            ByResref[entity.Resref] = entity;
        }

        protected override void OnCacheObjectRemoved(Area entity)
        {
            ByResref.Remove(entity.Resref);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public Area GetByID(Guid id)
        {
            return ByID[id];
        }

        public Area GetByResref(string resref)
        {
            return ByResref[resref];
        }

        public Area GetByResrefOrDefault(string resref)
        {
            if (!ByResref.ContainsKey(resref))
                return default;
            return ByResref[resref];
        }
    }
}
