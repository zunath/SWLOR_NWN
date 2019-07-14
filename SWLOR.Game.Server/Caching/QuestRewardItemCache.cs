using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class QuestRewardItemCache: CacheBase<QuestRewardItem>
    {
        protected override void OnCacheObjectSet(QuestRewardItem entity)
        {
        }

        protected override void OnCacheObjectRemoved(QuestRewardItem entity)
        {
        }
    }
}
