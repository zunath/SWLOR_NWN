using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class KeyItemCache: CacheBase<KeyItem>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, KeyItem entity)
        {
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, KeyItem entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public KeyItem GetByID(int id)
        {
            return ByID(id);
        }
    }
}
