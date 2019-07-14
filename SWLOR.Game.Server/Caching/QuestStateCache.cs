using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class QuestStateCache: CacheBase<QuestState>
    {
        protected override void OnCacheObjectSet(QuestState entity)
        {
        }

        protected override void OnCacheObjectRemoved(QuestState entity)
        {
        }
    }
}
