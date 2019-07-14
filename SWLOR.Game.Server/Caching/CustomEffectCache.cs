using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class CustomEffectCache: CacheBase<Data.Entity.CustomEffect>
    {
        protected override void OnCacheObjectSet(Data.Entity.CustomEffect entity)
        {
        }

        protected override void OnCacheObjectRemoved(Data.Entity.CustomEffect entity)
        {
        }

        public Data.Entity.CustomEffect GetByID(int id)
        {
            return ByID[id];
        }
    }
}
