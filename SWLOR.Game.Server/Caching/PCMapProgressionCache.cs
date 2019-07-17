using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCMapProgressionCache: CacheBase<PCMapProgression>
    {
        private Dictionary<Guid, Dictionary<string, PCMapProgression>> ByPlayerIDAndAreaResref { get; } = new Dictionary<Guid, Dictionary<string, PCMapProgression>>();

        protected override void OnCacheObjectSet(PCMapProgression entity)
        {
            SetEntityIntoDictionary(entity.PlayerID, entity.AreaResref, entity, ByPlayerIDAndAreaResref);
        }

        protected override void OnCacheObjectRemoved(PCMapProgression entity)
        {
            RemoveEntityFromDictionary(entity.PlayerID, entity.AreaResref, ByPlayerIDAndAreaResref);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCMapProgression GetByID(Guid id)
        {
            return (PCMapProgression)ByID[id].Clone();
        }

        public PCMapProgression GetByPlayerIDAndAreaResrefOrDefault(Guid playerID, string areaResref)
        {
            return GetEntityFromDictionaryOrDefault(playerID, areaResref, ByPlayerIDAndAreaResref);
        }
    }
}
