using SWLOR.Game.Server.Legacy.Data.Entity;

namespace SWLOR.Game.Server.Legacy.Caching
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
