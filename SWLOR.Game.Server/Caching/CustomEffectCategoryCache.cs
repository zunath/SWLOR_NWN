using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class CustomEffectCategoryCache: CacheBase<CustomEffectCategory>
    {
        public CustomEffectCategoryCache() 
            : base("CustomEffectCategory")
        {
        }

        protected override void OnCacheObjectSet(CustomEffectCategory entity)
        {
        }

        protected override void OnCacheObjectRemoved(CustomEffectCategory entity)
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
