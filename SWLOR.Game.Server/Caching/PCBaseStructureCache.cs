using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCBaseStructureCache: CacheBase<PCBaseStructure>
    {
        private Dictionary<Guid, List<PCBaseStructure>> ByPCBaseID { get; } = new Dictionary<Guid, List<PCBaseStructure>>();

        protected override void OnCacheObjectSet(PCBaseStructure entity)
        {
            SetEntityIntoDictionary(entity.PCBaseID, entity, ByPCBaseID);
        }

        protected override void OnCacheObjectRemoved(PCBaseStructure entity)
        {
            RemoveEntityFromDictionary(entity.PCBaseID, entity, ByPCBaseID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCBaseStructure GetByID(Guid id)
        {
            return ByID[id];
        }

        public IEnumerable<PCBaseStructure> GetAllByPCBaseID(Guid pcBaseID)
        {
            if(!ByPCBaseID.ContainsKey(pcBaseID))
                ByPCBaseID[pcBaseID] = new List<PCBaseStructure>();
            return ByPCBaseID[pcBaseID];
        }
    }
}
