using System;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class DiscordChatQueueCache: CacheBase<DiscordChatQueue>
    {
        protected override void OnCacheObjectSet(DiscordChatQueue entity)
        {
        }

        protected override void OnCacheObjectRemoved(DiscordChatQueue entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public DiscordChatQueue GetByID(Guid id)
        {
            return ByID[id];
        }
    }
}
