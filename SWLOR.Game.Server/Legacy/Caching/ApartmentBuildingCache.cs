using SWLOR.Game.Server.Legacy.Data.Entity;

namespace SWLOR.Game.Server.Legacy.Caching
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
