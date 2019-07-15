using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCObjectVisibilityCache: CacheBase<PCObjectVisibility>
    {
        private Dictionary<Guid, Dictionary<string, PCObjectVisibility>> ByPlayer { get; } = new Dictionary<Guid, Dictionary<string, PCObjectVisibility>>();

        protected override void OnCacheObjectSet(PCObjectVisibility entity)
        {
            SetEntityIntoDictionary(entity.PlayerID, entity.VisibilityObjectID, entity, ByPlayer);
        }

        protected override void OnCacheObjectRemoved(PCObjectVisibility entity)
        {
            RemoveEntityFromDictionary(entity.PlayerID, entity.VisibilityObjectID, ByPlayer);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCObjectVisibility GetByID(Guid id)
        {
            return ByID[id];
        }

        public PCObjectVisibility GetByPlayerIDAndVisibilityObjectIDOrDefault(Guid playerID, string visibilityObjectID)
        {
            return GetEntityFromDictionaryOrDefault(playerID, visibilityObjectID, ByPlayer);
        }
    }
}
