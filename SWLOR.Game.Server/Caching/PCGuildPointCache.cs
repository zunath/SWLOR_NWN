using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCGuildPointCache: CacheBase<PCGuildPoint>
    {
        private Dictionary<Guid, Dictionary<int, PCGuildPoint>> ByPlayerIDAndGuildID { get; } = new Dictionary<Guid, Dictionary<int, PCGuildPoint>>();

        protected override void OnCacheObjectSet(PCGuildPoint entity)
        {
            SetEntityIntoDictionary(entity.PlayerID, entity.GuildID, entity, ByPlayerIDAndGuildID);
        }

        protected override void OnCacheObjectRemoved(PCGuildPoint entity)
        {
            RemoveEntityFromDictionary(entity.PlayerID, entity.GuildID, ByPlayerIDAndGuildID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCGuildPoint GetByID(Guid id)
        {
            return (PCGuildPoint)ByID[id].Clone();
        }

        public PCGuildPoint GetByIDOrDefault(Guid id)
        {
            if (!ByID.ContainsKey(id))
                return default;
            return (PCGuildPoint)ByID[id].Clone();
        }

        public PCGuildPoint GetByPlayerIDAndGuildID(Guid playerID, int guildID)
        {
            return GetEntityFromDictionary(playerID, guildID, ByPlayerIDAndGuildID);
        }

        public PCGuildPoint GetByPlayerIDAndGuildIDOrDefault(Guid playerID, int guildID)
        {
            return GetEntityFromDictionaryOrDefault(playerID, guildID, ByPlayerIDAndGuildID);
        }
    }
}
