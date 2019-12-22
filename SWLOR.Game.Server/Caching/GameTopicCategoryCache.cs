using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class GameTopicCategoryCache: CacheBase<GameTopicCategory>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, GameTopicCategory entity)
        {
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, GameTopicCategory entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public GameTopicCategory GetByID(int id)
        {
            return ByID(id);
        }
    }
}
