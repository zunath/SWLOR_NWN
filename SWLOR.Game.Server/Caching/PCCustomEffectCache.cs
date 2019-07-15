using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCCustomEffectCache: CacheBase<PCCustomEffect>
    {
        private Dictionary<Guid, List<PCCustomEffect>> ByPlayer { get; } = new Dictionary<Guid, List<PCCustomEffect>>();

        protected override void OnCacheObjectSet(PCCustomEffect entity)
        {
            SetEntityIntoDictionary(entity.PlayerID, entity, ByPlayer);
        }

        protected override void OnCacheObjectRemoved(PCCustomEffect entity)
        {
            RemoveEntityFromDictionary(entity.PlayerID, entity, ByPlayer);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCCustomEffect GetByID(Guid id)
        {
            return ByID[id];
        }

        public PCCustomEffect GetByStancePerkOrDefault(Guid playerID, int stancePerkID)
        {
            if (!ByPlayer.ContainsKey(playerID))
                return default;

            return ByPlayer[playerID].SingleOrDefault(x => x.StancePerkID == stancePerkID);
        }

        public PCCustomEffect GetByPlayerStanceOrDefault(Guid playerID)
        {
            if (!ByPlayer.ContainsKey(playerID))
                return default;

            return ByPlayer[playerID].SingleOrDefault(x => x.StancePerkID != null);
        }

        public PCCustomEffect GetByPlayerIDAndCustomEffectIDOrDefault(Guid playerID, int customEffectID)
        {
            if (!ByPlayer.ContainsKey(playerID))
                return default;

            return ByPlayer[playerID].SingleOrDefault(x => x.PlayerID == playerID && x.CustomEffectID == customEffectID);
        }
    }
}
