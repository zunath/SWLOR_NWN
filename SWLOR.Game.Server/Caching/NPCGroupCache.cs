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
    }
}
