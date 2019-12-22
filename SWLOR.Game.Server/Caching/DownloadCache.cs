using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class DownloadCache: CacheBase<Download>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, Download entity)
        {
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, Download entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public Download GetByID(int id)
        {
            return ByID(id);
        }
    }
}
