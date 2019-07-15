using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCBasePermissionCache: CacheBase<PCBasePermission>
    {
        // Organized by PlayerID -> PCBasePermissionID
        private Dictionary<Guid, Dictionary<Guid, PCBasePermission>> ByPlayerID { get; } = new Dictionary<Guid, Dictionary<Guid, PCBasePermission>>();

        protected override void OnCacheObjectSet(PCBasePermission entity)
        {
            SetEntityIntoDictionary(entity.PlayerID, entity.ID, entity, ByPlayerID);

        }

        protected override void OnCacheObjectRemoved(PCBasePermission entity)
        {
            RemoveEntityFromDictionary(entity.PlayerID, entity.ID, ByPlayerID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        /// <summary>
        /// Returns a PCBasePermission by its unique ID.
        /// Throws KeyNotFound exception if ID doesn't exist.
        /// </summary>
        /// <param name="id">The unique iD to retrieve</param>
        /// <returns></returns>
        public PCBasePermission GetByID(Guid id)
        {
            return ByID[id];
        }

        public IEnumerable<PCBasePermission> GetAllByPlayerID(Guid id)
        {
            return ByPlayerID[id].Values;
        }

        public PCBasePermission GetByPlayerAndPCBaseIDOrDefault(Guid playerID, Guid pcBaseID)
        {
            return GetEntityFromDictionaryOrDefault(playerID, pcBaseID, ByPlayerID);
        }

        public PCBasePermission GetPublicPermissionOrDefault(Guid pcBaseID)
        {
            return ByID.Values.SingleOrDefault(x => x.PCBaseID == pcBaseID && x.IsPublicPermission);
        }

        public PCBasePermission GetPlayerPrivatePermissionOrDefault(Guid playerID, Guid pcBaseID)
        {
            if (!ByPlayerID.ContainsKey(playerID))
                return default;

            var permissions = ByPlayerID[playerID].Values;
            return permissions.SingleOrDefault(x => !x.IsPublicPermission && x.PCBaseID == pcBaseID);
        }
    }
}
