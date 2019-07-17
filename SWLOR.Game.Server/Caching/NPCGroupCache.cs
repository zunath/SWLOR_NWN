using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class NPCGroupCache: CacheBase<NPCGroup>
    {
        protected override void OnCacheObjectSet(NPCGroup entity)
        {
        }

        protected override void OnCacheObjectRemoved(NPCGroup entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public NPCGroup GetByID(int id)
        {
            return (NPCGroup)ByID[id].Clone();
        }

    }
}
