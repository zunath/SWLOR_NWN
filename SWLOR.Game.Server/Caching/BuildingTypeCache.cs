using System;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class BuildingTypeCache: CacheBase<BuildingType>
    {
        protected override void OnCacheObjectSet(BuildingType entity)
        {
        }

        protected override void OnCacheObjectRemoved(BuildingType entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public BuildingType GetByID(int id)
        {
            return (BuildingType)ByID[id].Clone();
        }
    }
}
