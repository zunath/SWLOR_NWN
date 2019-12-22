using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class CooldownCategoryCache: CacheBase<CooldownCategory>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, CooldownCategory entity)
        {
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, CooldownCategory entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public CooldownCategory GetByID(int id)
        {
            return ByID(id);
        }

        public CooldownCategory GetByIDOrDefault(int id)
        {
            if (Exists(id))
                return ByID(id);
            else return default;
        }

    }
}
