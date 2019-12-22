using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class ServerConfigurationCache: CacheBase<ServerConfiguration>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, ServerConfiguration entity)
        {
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, ServerConfiguration entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public ServerConfiguration Get()
        {
            return ByID(1);
        }
    }
}
