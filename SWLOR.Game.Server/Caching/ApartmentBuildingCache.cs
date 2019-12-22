using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class ApartmentBuildingCache: CacheBase<ApartmentBuilding>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, ApartmentBuilding entity)
        {
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, ApartmentBuilding entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public ApartmentBuilding GetByID(int id)
        {
            return ByID(id);
        }
    }
}
