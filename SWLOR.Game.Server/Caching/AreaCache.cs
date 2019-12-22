using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Caching
{
    public class AreaCache: CacheBase<Area>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, Area entity)
        {
            SetIndexByKey(entity.Resref, id);
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, Area entity)
        {
            RemoveByIndex(entity.Resref);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public Area GetByID(Guid id)
        {
            return ByID(id);
        }

        public Area GetByResref(string resref)
        {
            return GetByIndex(resref);
        }

        public Area GetByResrefOrDefault(string resref)
        {
            if (!ExistsByIndex(resref))
                return default;

            return GetByIndex(resref);
        }
    }
}
