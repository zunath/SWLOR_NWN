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

        protected override void OnSubscribeEvents()
        {
        }

        public ServerConfiguration Get()
        {
            return (ServerConfiguration)ByID[1].Clone();
        }
    }
}
