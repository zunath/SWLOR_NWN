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
            ByResref[entity.Resref] = (Area)entity.Clone();
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
            return (Area)ByID[id].Clone();
        }

        public Area GetByResref(string resref)
        {
            return (Area)ByResref[resref].Clone();
        }

        public Area GetByResrefOrDefault(string resref)
        {
            if (!ByResref.ContainsKey(resref))
                return default;

            return (Area)ByResref[resref].Clone();
        }
    }
}
