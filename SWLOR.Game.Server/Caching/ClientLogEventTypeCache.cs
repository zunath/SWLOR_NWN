using System;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class ClientLogEventTypeCache: CacheBase<ModuleEventType>
    {
        protected override void OnCacheObjectSet(ModuleEventType entity)
        {
        }

        protected override void OnCacheObjectRemoved(ModuleEventType entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public ModuleEventType GetByID(int id)
        {
            return (ModuleEventType)ByID[id].Clone();
        }
    }
}
