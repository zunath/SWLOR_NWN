using System;
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

        protected override void OnSubscribeEvents()
        {
        }

        public ChatLog GetByID(Guid id)
        {
            return (ChatLog)ByID[id].Clone();
        }
    }
}
