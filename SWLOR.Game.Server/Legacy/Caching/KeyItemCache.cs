using SWLOR.Game.Server.Legacy.Data.Entity;

namespace SWLOR.Game.Server.Legacy.Caching
{
    public class KeyItemCache: CacheBase<KeyItem>
    {
        protected override void OnCacheObjectSet(KeyItem entity)
        {
        }

        protected override void OnCacheObjectRemoved(KeyItem entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public KeyItem GetByID(int id)
        {
            return (KeyItem)ByID[id].Clone();
        }
    }
}
