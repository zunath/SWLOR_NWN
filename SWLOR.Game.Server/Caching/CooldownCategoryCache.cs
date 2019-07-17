using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class CooldownCategoryCache: CacheBase<CooldownCategory>
    {
        protected override void OnCacheObjectSet(CooldownCategory entity)
        {
        }

        protected override void OnCacheObjectRemoved(CooldownCategory entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public CooldownCategory GetByID(int id)
        {
            return (CooldownCategory)ByID[id].Clone();
        }

        public CooldownCategory GetByIDOrDefault(int id)
        {
            if (ByID.ContainsKey(id))
                return (CooldownCategory)ByID[id].Clone();
            else return default;
        }

    }
}
