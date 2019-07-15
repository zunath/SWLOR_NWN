using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
