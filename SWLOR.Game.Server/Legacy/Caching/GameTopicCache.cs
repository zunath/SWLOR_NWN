using SWLOR.Game.Server.Legacy.Data.Entity;

namespace SWLOR.Game.Server.Legacy.Caching
{
    public class GameTopicCache: CacheBase<GameTopic>
    {
        protected override void OnCacheObjectSet(GameTopic entity)
        {
        }

        protected override void OnCacheObjectRemoved(GameTopic entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public GameTopic GetByID(int id)
        {
            return (GameTopic)ByID[id].Clone();
        }
    }
}
