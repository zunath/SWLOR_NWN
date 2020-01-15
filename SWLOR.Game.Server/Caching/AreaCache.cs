using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Caching
{
    public class AreaCache: CacheBase<Area>
    {
        private const string _resrefIndex = "Resref";

        public AreaCache()
            : base("Area")
        {
        }
        protected override void OnCacheObjectSet(Area entity)
        {
            SetIntoIndex(_resrefIndex, entity.Resref, entity);
        }

        protected override void OnCacheObjectRemoved(Area entity)
        {
            RemoveFromIndex(_resrefIndex, entity.Resref);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public Area GetByID(string resref)
        {
            return ByID(resref);
        }

        public Area GetByResref(string resref)
        {
            return GetFromIndex(_resrefIndex, resref);
        }

        public Area GetByResrefOrDefault(string resref)
        {
            if (!ExistsByIndex(_resrefIndex, resref))
                return default;

            return GetFromIndex(_resrefIndex, resref);
        }
    }
}
