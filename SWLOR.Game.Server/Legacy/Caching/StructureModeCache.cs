using SWLOR.Game.Server.Legacy.Data.Entity;

namespace SWLOR.Game.Server.Legacy.Caching
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
