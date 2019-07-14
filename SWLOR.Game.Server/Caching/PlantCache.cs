using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PlantCache: CacheBase<Plant>
    {
        protected override void OnCacheObjectSet(Plant entity)
        {
        }

        protected override void OnCacheObjectRemoved(Plant entity)
        {
        }

        public Plant GetByID(int id)
        {
            return ByID[id];
        }
    }
}
