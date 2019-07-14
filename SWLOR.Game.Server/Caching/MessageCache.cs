using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class MessageCache: CacheBase<Message>
    {
        protected override void OnCacheObjectSet(Message entity)
        {
        }

        protected override void OnCacheObjectRemoved(Message entity)
        {
        }
    }
}
