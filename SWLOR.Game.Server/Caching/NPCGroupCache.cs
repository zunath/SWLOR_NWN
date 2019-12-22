using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class NPCGroupCache: CacheBase<NPCGroup>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, NPCGroup entity)
        {
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, NPCGroup entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public NPCGroup GetByID(int id)
        {
            return ByID(id);
        }

    }
}
