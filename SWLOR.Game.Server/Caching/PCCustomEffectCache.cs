using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCCustomEffectCache: CacheBase<PCCustomEffect>
    {
        public PCCustomEffectCache() 
            : base("PCCustomEffect")
        {
        }

        private const string ByPlayerIDIndex = "ByPlayerID";

        protected override void OnCacheObjectSet(PCCustomEffect entity)
        {
            SetIntoListIndex(ByPlayerIDIndex, entity.PlayerID.ToString(), entity);
        }

        protected override void OnCacheObjectRemoved(PCCustomEffect entity)
        {
            RemoveFromListIndex(ByPlayerIDIndex, entity.PlayerID.ToString(), entity);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCCustomEffect GetByID(Guid id)
        {
            return ByID(id);
        }

        public PCCustomEffect GetByStancePerkOrDefault(Guid playerID, int stancePerkID)
        {
            if (!ExistsByIndex(ByPlayerIDIndex, playerID.ToString()))
                return default;

            return GetFromListIndex(ByPlayerIDIndex, playerID.ToString())
                .SingleOrDefault(x => x.StancePerkID == stancePerkID);
        }

        public PCCustomEffect GetByPlayerStanceOrDefault(Guid playerID)
        {
            if (!ExistsByIndex(ByPlayerIDIndex, playerID.ToString()))
                return default;

            return GetFromListIndex(ByPlayerIDIndex, playerID.ToString())
                .SingleOrDefault(x => x.StancePerkID != null);
        }

        public PCCustomEffect GetByPlayerIDAndCustomEffectIDOrDefault(Guid playerID, int customEffectID)
        {
            if (!ExistsByIndex(ByPlayerIDIndex, playerID.ToString()))
                return default;

            return GetFromListIndex(ByPlayerIDIndex, playerID.ToString())
                .SingleOrDefault(x => x.CustomEffectID == customEffectID);
        }

        public IEnumerable<PCCustomEffect> GetAllByPlayerID(Guid playerID)
        {
            if (!ExistsByIndex(ByPlayerIDIndex, playerID.ToString()))
                return new List<PCCustomEffect>();

            return GetFromListIndex(ByPlayerIDIndex, playerID.ToString());
        }

        public IEnumerable<PCCustomEffect> GetAllByPCCustomEffectID(IEnumerable<Guid> pcCustomEffectIDs)
        {
            var list = new List<PCCustomEffect>();
            foreach (var pcCustomEffectID in pcCustomEffectIDs)
            {
                list.Add( ByID(pcCustomEffectID));
            }

            return list;
        }
    }
}
