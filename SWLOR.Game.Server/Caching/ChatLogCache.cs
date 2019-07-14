using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class ChatLogCache: CacheBase<ChatLog>
    {
        protected override void OnCacheObjectSet(ChatLog entity)
        {
        }

        protected override void OnCacheObjectRemoved(ChatLog entity)
        {
        }
    }
}
