using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class GameTopicCache: CacheBase<GameTopic>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, GameTopic entity)
        {
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, GameTopic entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public GameTopic GetByID(int id)
        {
            return ByID(id);
        }
    }
}
