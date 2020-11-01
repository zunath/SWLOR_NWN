using SWLOR.Game.Server.Legacy.Data.Entity;

namespace SWLOR.Game.Server.Legacy.Caching
{
    public class DownloadCache: CacheBase<Download>
    {
        protected override void OnCacheObjectSet(Download entity)
        {
        }

        protected override void OnCacheObjectRemoved(Download entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public Download GetByID(int id)
        {
            return (Download)ByID[id].Clone();
        }
    }
}
