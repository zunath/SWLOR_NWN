using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class QuestKillTargetCache: CacheBase<QuestKillTarget>
    {
        protected override void OnCacheObjectSet(QuestKillTarget entity)
        {
        }

        protected override void OnCacheObjectRemoved(QuestKillTarget entity)
        {
        }
    }
}
