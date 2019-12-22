using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class ChatChannelCache: CacheBase<ChatChannel>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, ChatChannel entity)
        {
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, ChatChannel entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public ChatChannel GetByID(int id)
        {
            return ByID(id);
        }
    }
}
