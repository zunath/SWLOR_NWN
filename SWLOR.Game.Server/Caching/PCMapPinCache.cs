using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCMapPinCache: CacheBase<PCMapPin>
    {
        private Dictionary<Guid, Dictionary<Guid, PCMapPin>> ByPlayerID { get; } = new Dictionary<Guid, Dictionary<Guid, PCMapPin>>();

        protected override void OnCacheObjectSet(PCMapPin entity)
        {
            SetEntityIntoDictionary(entity.PlayerID, entity.ID, entity, ByPlayerID);
        }

        protected override void OnCacheObjectRemoved(PCMapPin entity)
        {
            RemoveEntityFromDictionary(entity.PlayerID, entity.ID, ByPlayerID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCMapPin GetByID(Guid id)
        {
            return ByID[id];
        }

        public IEnumerable<PCMapPin> GetAllByPlayerID(Guid playerID)
        {
            if(!ByPlayerID.ContainsKey(playerID))
                return new List<PCMapPin>();

            return ByPlayerID[playerID].Values;
        }
    }
}
