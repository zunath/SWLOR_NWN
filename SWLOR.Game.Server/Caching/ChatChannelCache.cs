using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class ChatChannelCache: CacheBase<ChatChannel>
    {
        protected override void OnCacheObjectSet(ChatChannel entity)
        {
        }

        protected override void OnCacheObjectRemoved(ChatChannel entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public ChatChannel GetByID(int id)
        {
            return (ChatChannel)ByID[id].Clone();
        }
    }
}
