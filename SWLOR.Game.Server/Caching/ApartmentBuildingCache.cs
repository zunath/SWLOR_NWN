using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class ApartmentBuildingCache: CacheBase<ApartmentBuilding>
    {
        protected override void OnCacheObjectSet(ApartmentBuilding entity)
        {
        }

        protected override void OnCacheObjectRemoved(ApartmentBuilding entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public ApartmentBuilding GetByID(int id)
        {
            return (ApartmentBuilding)ByID[id].Clone();
        }
    }
}
