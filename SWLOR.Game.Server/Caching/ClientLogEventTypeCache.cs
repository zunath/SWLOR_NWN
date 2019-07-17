using System;
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

        protected override void OnSubscribeEvents()
        {
        }

        public ClientLogEventType GetByID(int id)
        {
            return (ClientLogEventType)ByID[id].Clone();
        }
    }
}
