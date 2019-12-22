using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class CustomEffectCache: CacheBase<Data.Entity.CustomEffect>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, Data.Entity.CustomEffect entity)
        {
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, Data.Entity.CustomEffect entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public Data.Entity.CustomEffect GetByID(int id)
        {
            return ByID(id);
        }
    }
}
