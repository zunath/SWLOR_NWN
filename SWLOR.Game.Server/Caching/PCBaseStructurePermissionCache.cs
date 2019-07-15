using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCBaseStructurePermissionCache: CacheBase<PCBaseStructurePermission>
    {
        // Primary Index: PlayerID
        // Secondary Index: PCBaseStructurePermissionID
        private Dictionary<Guid, Dictionary<Guid, PCBaseStructurePermission>> ByPlayerID { get; } = new Dictionary<Guid, Dictionary<Guid, PCBaseStructurePermission>>();

        // Primary INdex: PCBaseStructureID
        // Secondary Index: PCBaseStructurePermissionID
        private Dictionary<Guid, Dictionary<Guid, PCBaseStructurePermission>> ByPCBaseStructureID { get; } = new Dictionary<Guid, Dictionary<Guid, PCBaseStructurePermission>>();

        protected override void OnCacheObjectSet(PCBaseStructurePermission entity)
        {
            SetEntityIntoDictionary(entity.PlayerID, entity.ID, entity, ByPlayerID);
            SetEntityIntoDictionary(entity.PCBaseStructureID, entity.ID, entity, ByPCBaseStructureID);
        }

        protected override void OnCacheObjectRemoved(PCBaseStructurePermission entity)
        {
            RemoveEntityFromDictionary(entity.PlayerID, entity.ID, ByPlayerID);
            RemoveEntityFromDictionary(entity.PCBaseStructureID, entity.ID, ByPCBaseStructureID);
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

        public PCBaseStructurePermission GetPublicPermissionOrDefault(Guid pcBaseStructureID)
        {
            return ByID.Values.SingleOrDefault(x => x.PCBaseStructureID == pcBaseStructureID && x.IsPublicPermission);
        }

        public PCBaseStructurePermission GetPlayerPrivatePermissionOrDefault(Guid playerID, Guid pcBaseStructureID)
        {
            if (!ByPlayerID.ContainsKey(playerID))
                return default;

            var permissions = ByPlayerID[playerID].Values;
            return permissions.SingleOrDefault(x => !x.IsPublicPermission && x.PCBaseStructureID == pcBaseStructureID);
        }

        public IEnumerable<PCBaseStructurePermission> GetAllByPCBaseStructureID(Guid pcBaseStructureID)
        {
            if (!ByPCBaseStructureID.ContainsKey(pcBaseStructureID))
            {
                ByPCBaseStructureID[pcBaseStructureID] = new Dictionary<Guid, PCBaseStructurePermission>(); 
            }

            return ByPCBaseStructureID[pcBaseStructureID].Values;
        }
    }
}
