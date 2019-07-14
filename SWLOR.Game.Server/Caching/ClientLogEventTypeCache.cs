using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class ClientLogEventTypeCache: CacheBase<ClientLogEventType>
    {
        protected override void OnCacheObjectSet(ClientLogEventType entity)
        {
        }

        protected override void OnCacheObjectRemoved(ClientLogEventType entity)
        {
        }
    }
}
