using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class QuestPrerequisiteCache: CacheBase<QuestPrerequisite>
    {
        protected override void OnCacheObjectSet(QuestPrerequisite entity)
        {
        }

        protected override void OnCacheObjectRemoved(QuestPrerequisite entity)
        {
        }
    }
}
