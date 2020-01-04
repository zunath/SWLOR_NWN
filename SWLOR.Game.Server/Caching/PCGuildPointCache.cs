using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Caching
{
    public class PCGuildPointCache: CacheBase<PCGuildPoint>
    {
        public PCGuildPointCache() 
            : base("PCGuildPoint")
        {
        }

        protected override void OnCacheObjectSet(PCGuildPoint entity)
        {
            SetIntoIndex(entity.PlayerID.ToString(), entity.GuildID.ToString(), entity);
        }

        protected override void OnCacheObjectRemoved(PCGuildPoint entity)
        {
            RemoveFromIndex(entity.PlayerID.ToString(), entity.GuildID.ToString());
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCGuildPoint GetByID(Guid id)
        {
            return ByID(id);
        }

        public PCGuildPoint GetByIDOrDefault(Guid id)
        {
            if (!Exists(id))
                return default;
            return ByID(id);
        }

        public PCGuildPoint GetByPlayerIDAndGuildID(Guid playerID, GuildType guildID)
        {
            return GetFromIndex(playerID.ToString(), guildID.ToString());
        }

        public PCGuildPoint GetByPlayerIDAndGuildIDOrDefault(Guid playerID, GuildType guildID)
        {
            if (!ExistsByIndex(playerID.ToString(), guildID.ToString()))
                return default;

            return GetFromIndex(playerID.ToString(), guildID.ToString());
        }
    }
}
