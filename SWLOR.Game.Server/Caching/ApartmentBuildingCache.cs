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
    }
}
