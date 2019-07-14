using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class QuestRequiredItemCache: CacheBase<QuestRequiredItem>
    {
        protected override void OnCacheObjectSet(QuestRequiredItem entity)
        {
        }

        protected override void OnCacheObjectRemoved(QuestRequiredItem entity)
        {
        }
    }
}
