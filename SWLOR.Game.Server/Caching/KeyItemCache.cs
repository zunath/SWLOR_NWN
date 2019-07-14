using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class KeyItemCache: CacheBase<KeyItem>
    {
        protected override void OnCacheObjectSet(KeyItem entity)
        {
        }

        protected override void OnCacheObjectRemoved(KeyItem entity)
        {
        }

        public KeyItem GetByID(int id)
        {
            return ByID[id];
        }
    }
}
