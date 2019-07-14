using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class QuestTypeCache: CacheBase<QuestType>
    {
        protected override void OnCacheObjectSet(QuestType entity)
        {
        }

        protected override void OnCacheObjectRemoved(QuestType entity)
        {
        }
    }
}
