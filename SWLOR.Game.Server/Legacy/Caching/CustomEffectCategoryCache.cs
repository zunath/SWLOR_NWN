using SWLOR.Game.Server.Legacy.Data.Entity;

namespace SWLOR.Game.Server.Legacy.Caching
{
    public class CustomEffectCategoryCache: CacheBase<CustomEffectCategory>
    {
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
            return (CustomEffectCategory)ByID[id].Clone();
        }
    }
}
