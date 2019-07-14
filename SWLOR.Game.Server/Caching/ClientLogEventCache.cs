using System;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class ClientLogEventCache: CacheBase<ClientLogEvent>
    {
        protected override void OnCacheObjectSet(ClientLogEvent entity)
        {
        }

        protected override void OnCacheObjectRemoved(ClientLogEvent entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public ClientLogEvent GetByID(Guid id)
        {
            return ByID[id];
        }
    }
}
