using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCBaseStructurePermissionCache: CacheBase<PCBaseStructurePermission>
    {
        private Dictionary<Guid, Dictionary<Guid, PCBaseStructurePermission>> ByPlayerID { get; } = new Dictionary<Guid, Dictionary<Guid, PCBaseStructurePermission>>();

        protected override void OnCacheObjectSet(PCBaseStructurePermission entity)
        {
            SetEntityIntoDictionary(entity.PlayerID, entity.ID, entity, ByPlayerID);
        }

        protected override void OnCacheObjectRemoved(PCBaseStructurePermission entity)
        {
            RemoveEntityFromDictionary(entity.PlayerID, entity.ID, ByPlayerID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCBaseStructurePermission GetByID(Guid id)
        {
            return ByID[id];
        }

        public IEnumerable<PCBaseStructurePermission> GetAllByPlayerID(Guid playerID)
        {
            if (!ByPlayerID.ContainsKey(playerID))
            {
                ByPlayerID[playerID] = new Dictionary<Guid, PCBaseStructurePermission>();
            }

            return ByPlayerID[playerID].Values;
        }
    }
}
