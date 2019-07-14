using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class QuestRequiredKeyItemCache: CacheBase<QuestRequiredKeyItem>
    {
        protected override void OnCacheObjectSet(QuestRequiredKeyItem entity)
        {
        }

        protected override void OnCacheObjectRemoved(QuestRequiredKeyItem entity)
        {
        }
    }
}
