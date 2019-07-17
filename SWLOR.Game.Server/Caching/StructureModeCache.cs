using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class StructureModeCache: CacheBase<StructureMode>
    {
        protected override void OnCacheObjectSet(StructureMode entity)
        {
        }

        protected override void OnCacheObjectRemoved(StructureMode entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public StructureMode GetByID(int id)
        {
            return (StructureMode)ByID[id].Clone();
        }
    }
}
