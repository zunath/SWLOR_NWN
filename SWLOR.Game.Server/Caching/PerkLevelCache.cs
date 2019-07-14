using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PerkLevelCache: CacheBase<PerkLevel>
    {
        protected override void OnCacheObjectSet(PerkLevel entity)
        {
        }

        protected override void OnCacheObjectRemoved(PerkLevel entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PerkLevel GetByID(int id)
        {
            return ByID[id];
        }
    }
}
