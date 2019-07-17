using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
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
