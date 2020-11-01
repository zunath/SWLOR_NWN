using SWLOR.Game.Server.Legacy.Data.Entity;

namespace SWLOR.Game.Server.Legacy.Caching
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
