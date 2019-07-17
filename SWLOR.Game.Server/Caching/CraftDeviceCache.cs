using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class CraftDeviceCache: CacheBase<CraftDevice>
    {
        protected override void OnCacheObjectSet(CraftDevice entity)
        {
        }

        protected override void OnCacheObjectRemoved(CraftDevice entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public CraftDevice GetByID(int id)
        {
            return (CraftDevice)ByID[id].Clone();
        }
    }
}
