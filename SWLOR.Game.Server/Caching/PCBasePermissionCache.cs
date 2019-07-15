using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCBasePermissionCache: CacheBase<PCBasePermission>
    {
        // Primary Index: PlayerID
        // Secondary Index: PCBasePermissionID
        private Dictionary<Guid, Dictionary<Guid, PCBasePermission>> ByPlayerID { get; } = new Dictionary<Guid, Dictionary<Guid, PCBasePermission>>();
        
        // Primary Index: PCBaseID
        // Secondary Index: PlayerID
        // Only includes private (non-IsPublic) records.
        private Dictionary<Guid, Dictionary<Guid, PCBasePermission>> ByPCBaseIDPrivate { get; } = new Dictionary<Guid, Dictionary<Guid, PCBasePermission>>();

        protected override void OnCacheObjectSet(PCBasePermission entity)
        {
            SetEntityIntoDictionary(entity.PlayerID, entity.ID, entity, ByPlayerID);

            if(!entity.IsPublicPermission)
                SetEntityIntoDictionary(entity.PCBaseID, entity.PlayerID, entity, ByPCBaseIDPrivate);
        }

        protected override void OnCacheObjectRemoved(PCBasePermission entity)
        {
            RemoveEntityFromDictionary(entity.PlayerID, entity.ID, ByPlayerID);

            if(!entity.IsPublicPermission)
                RemoveEntityFromDictionary(entity.PCBaseID, entity.PlayerID, ByPCBaseIDPrivate);

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

        public IEnumerable<PCBasePermission> GetAllByHasPrivatePermissionToBase(Guid pcBaseID)
        {
            return ByPCBaseIDPrivate[pcBaseID].Values;
        }
    }
}
