using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PerkCategoryCache: CacheBase<PerkCategory>
    {
        protected override void OnCacheObjectSet(PerkCategory entity)
        {
        }

        protected override void OnCacheObjectRemoved(PerkCategory entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PerkCategory GetByID(int id)
        {
            return ByID[id];
        }
    }
}
