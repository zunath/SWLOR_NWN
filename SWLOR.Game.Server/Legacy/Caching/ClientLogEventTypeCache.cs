using SWLOR.Game.Server.Legacy.Data.Entity;

namespace SWLOR.Game.Server.Legacy.Caching
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
