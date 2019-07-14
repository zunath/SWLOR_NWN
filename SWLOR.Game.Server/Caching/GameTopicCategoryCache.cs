using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class GameTopicCategoryCache: CacheBase<GameTopicCategory>
    {
        protected override void OnCacheObjectSet(GameTopicCategory entity)
        {
        }

        protected override void OnCacheObjectRemoved(GameTopicCategory entity)
        {
        }
    }
}
