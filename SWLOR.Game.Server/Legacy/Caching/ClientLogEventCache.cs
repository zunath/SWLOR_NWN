using System;
using SWLOR.Game.Server.Legacy.Data.Entity;

namespace SWLOR.Game.Server.Legacy.Caching
{
    public class ClientLogEventCache: CacheBase<ModuleEvent>
    {
        protected override void OnCacheObjectSet(ModuleEvent entity)
        {
        }

        protected override void OnCacheObjectRemoved(ModuleEvent entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public ModuleEvent GetByID(Guid id)
        {
            return (ModuleEvent)ByID[id].Clone();
        }
    }
}
