using SWLOR.Game.Server.Legacy.Data.Entity;

namespace SWLOR.Game.Server.Legacy.Caching
{
    public class GameTopicCategoryCache: CacheBase<GameTopicCategory>
    {
        protected override void OnCacheObjectSet(GameTopicCategory entity)
        {
        }

        protected override void OnCacheObjectRemoved(GameTopicCategory entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public GameTopicCategory GetByID(int id)
        {
            return (GameTopicCategory)ByID[id].Clone();
        }
    }
}
