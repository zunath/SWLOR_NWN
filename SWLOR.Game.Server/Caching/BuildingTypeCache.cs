using System;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class BuildingTypeCache: CacheBase<BuildingType>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, BuildingType entity)
        {
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, BuildingType entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public BuildingType GetByID(int id)
        {
            return ByID(id);
        }
    }
}
