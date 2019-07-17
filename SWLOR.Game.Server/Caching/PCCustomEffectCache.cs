using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCCustomEffectCache: CacheBase<PCCustomEffect>
    {
        private Dictionary<Guid, Dictionary<Guid, PCCustomEffect>> ByPlayer { get; } = new Dictionary<Guid, Dictionary<Guid, PCCustomEffect>>();

        protected override void OnCacheObjectSet(PCCustomEffect entity)
        {
            SetEntityIntoDictionary(entity.PlayerID, entity.ID, entity, ByPlayer);
        }

        protected override void OnCacheObjectRemoved(PCCustomEffect entity)
        {
            RemoveEntityFromDictionary(entity.PlayerID, entity.ID, ByPlayer);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCCustomEffect GetByID(Guid id)
        {
            return (PCCustomEffect)ByID[id].Clone();
        }

        public PCCustomEffect GetByStancePerkOrDefault(Guid playerID, int stancePerkID)
        {
            if (!ByPlayer.ContainsKey(playerID))
                return default;

            return (PCCustomEffect)ByPlayer[playerID].Values.SingleOrDefault(x => x.StancePerkID == stancePerkID)?.Clone();
        }

        public PCCustomEffect GetByPlayerStanceOrDefault(Guid playerID)
        {
            if (!ByPlayer.ContainsKey(playerID))
                return default;

            return (PCCustomEffect)ByPlayer[playerID].Values.SingleOrDefault(x => x.StancePerkID != null)?.Clone();
        }

        public PCCustomEffect GetByPlayerIDAndCustomEffectIDOrDefault(Guid playerID, int customEffectID)
        {
            if (!ByPlayer.ContainsKey(playerID))
                return default;

            return (PCCustomEffect)ByPlayer[playerID].Values.SingleOrDefault(x => x.PlayerID == playerID && x.CustomEffectID == customEffectID)?.Clone();
        }

        public IEnumerable<PCCustomEffect> GetAllByPlayerID(Guid playerID)
        {
            if(!ByPlayer.ContainsKey(playerID))
                return new List<PCCustomEffect>();

            var list = new List<PCCustomEffect>();

            foreach(var record in ByPlayer[playerID].Values)
            {
                list.Add((PCCustomEffect)record.Clone());
            }

            return list;
        }

        public IEnumerable<PCCustomEffect> GetAllByPCCustomEffectID(IEnumerable<Guid> pcCustomEffectIDs)
        {
            var list = new List<PCCustomEffect>();
            foreach (var pcCustomEffectID in pcCustomEffectIDs)
            {
                list.Add((PCCustomEffect)ByID[pcCustomEffectID].Clone());
            }

            return list;
        }
    }
}
