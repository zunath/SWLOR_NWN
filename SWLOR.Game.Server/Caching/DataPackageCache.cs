using System;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class DataPackageCache: CacheBase<DataPackage>
    {
        protected override void OnCacheObjectSet(DataPackage entity)
        {
        }

        protected override void OnCacheObjectRemoved(DataPackage entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public DataPackage GetByID(Guid id)
        {
            return ByID[id];
        }
    }
}
