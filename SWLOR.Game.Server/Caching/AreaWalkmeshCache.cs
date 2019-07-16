using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class AreaWalkmeshCache: CacheBase<AreaWalkmesh>
    {
        private Dictionary<Guid, Dictionary<Guid, AreaWalkmesh>> ByAreaID { get; } = new Dictionary<Guid, Dictionary<Guid, AreaWalkmesh>>();

        protected override void OnCacheObjectSet(AreaWalkmesh entity)
        {
            SetEntityIntoDictionary(entity.AreaID, entity.ID, entity, ByAreaID);
        }

        protected override void OnCacheObjectRemoved(AreaWalkmesh entity)
        {
            RemoveEntityFromDictionary(entity.AreaID, entity.ID, ByAreaID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public AreaWalkmesh GetByID(Guid id)
        {
            return ByID[id];
        }

        public IEnumerable<AreaWalkmesh> GetAllByAreaID(Guid areaID)
        {
            return ByAreaID[areaID].Values;
        }
    }
}
