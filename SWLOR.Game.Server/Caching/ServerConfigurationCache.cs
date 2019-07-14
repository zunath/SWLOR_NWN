using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class ServerConfigurationCache: CacheBase<ServerConfiguration>
    {
        protected override void OnCacheObjectSet(ServerConfiguration entity)
        {
        }

        protected override void OnCacheObjectRemoved(ServerConfiguration entity)
        {
        }

        public ServerConfiguration Get()
        {
            return ByID[1];
        }
    }
}
