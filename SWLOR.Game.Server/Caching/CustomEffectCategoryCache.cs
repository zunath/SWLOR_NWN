using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class CustomEffectCategoryCache: CacheBase<CustomEffectCategory>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, CustomEffectCategory entity)
        {
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, CustomEffectCategory entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public CustomEffectCategory GetByID(int id)
        {
            return ByID(id);
        }
    }
}
