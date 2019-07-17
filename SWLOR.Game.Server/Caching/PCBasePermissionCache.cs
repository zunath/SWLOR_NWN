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

        // Primary Index: PCBaseID
        // Secondary Index: PCBasePermissionID
        // Includes ALL records, both public and private.
        private Dictionary<Guid, Dictionary<Guid, PCBasePermission>> ByPCBaseIDAll { get; } = new Dictionary<Guid, Dictionary<Guid, PCBasePermission>>();

        protected override void OnCacheObjectSet(PCBasePermission entity)
        {
            SetEntityIntoDictionary(entity.PlayerID, entity.ID, entity, ByPlayerID);

            if(!entity.IsPublicPermission)
                SetEntityIntoDictionary(entity.PCBaseID, entity.PlayerID, entity, ByPCBaseIDPrivate);

            SetEntityIntoDictionary(entity.PCBaseID, entity.ID, entity, ByPCBaseIDAll);
        }

        protected override void OnCacheObjectRemoved(PCBasePermission entity)
        {
            RemoveEntityFromDictionary(entity.PlayerID, entity.ID, ByPlayerID);

            if(!entity.IsPublicPermission)
                RemoveEntityFromDictionary(entity.PCBaseID, entity.PlayerID, ByPCBaseIDPrivate);

            RemoveEntityFromDictionary(entity.PCBaseID, entity.ID, ByPCBaseIDAll);
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
            return (PCBasePermission)ByID[id].Clone();
        }

        public IEnumerable<PCBasePermission> GetAllByPlayerID(Guid id)
        {
            var list = new List<PCBasePermission>();
            if (!ByPlayerID.ContainsKey(id))
                return list;

            foreach (var pcBasePermission in ByPlayerID[id].Values)
            {
                list.Add((PCBasePermission)pcBasePermission.Clone());
            }

            return list;
        }

        public PCBasePermission GetByPlayerAndPCBaseIDOrDefault(Guid playerID, Guid pcBaseID)
        {
            return GetEntityFromDictionaryOrDefault(playerID, pcBaseID, ByPlayerID);
        }

        public PCBasePermission GetPublicPermissionOrDefault(Guid pcBaseID)
        {
            return (PCBasePermission)ByID.Values.SingleOrDefault(x => x.PCBaseID == pcBaseID && x.IsPublicPermission)?.Clone();
        }

        public PCBasePermission GetPlayerPrivatePermissionOrDefault(Guid playerID, Guid pcBaseID)
        {
            if (!ByPlayerID.ContainsKey(playerID))
                return default;

            var permissions = ByPlayerID[playerID].Values;
            return (PCBasePermission)permissions.SingleOrDefault(x => !x.IsPublicPermission && x.PCBaseID == pcBaseID)?.Clone();
        }

        public IEnumerable<PCBasePermission> GetAllByHasPrivatePermissionToBase(Guid pcBaseID)
        {
            var list = new List<PCBasePermission>();
            if (!ByPCBaseIDPrivate.ContainsKey(pcBaseID))
                return list;

            foreach (var record in ByPCBaseIDPrivate[pcBaseID].Values)
            {
                list.Add( (PCBasePermission)record.Clone());
            }

            return list;
        }

        public IEnumerable<PCBasePermission> GetAllPermissionsByPCBaseID(Guid pcBaseID)
        {
            var list = new List<PCBasePermission>();
            if (!ByPCBaseIDAll.ContainsKey(pcBaseID))
                return list;

            foreach (var record in ByPCBaseIDAll[pcBaseID].Values)
            {
                list.Add((PCBasePermission)record.Clone());
            }
            return list;
        }
        
    }
}
