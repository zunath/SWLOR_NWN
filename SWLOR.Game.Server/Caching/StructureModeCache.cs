using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class StructureModeCache: CacheBase<StructureMode>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, StructureMode entity)
        {
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, StructureMode entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public StructureMode GetByID(int id)
        {
            return ByID(id);
        }
    }
}
