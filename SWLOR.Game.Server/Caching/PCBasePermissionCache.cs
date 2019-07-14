using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCBasePermissionCache: CacheBase<PCBasePermission>
    {
        // Organized by PlayerID -> PCBasePermissionID
        private Dictionary<Guid, Dictionary<Guid, PCBasePermission>> ByPlayerIDList { get; } = new Dictionary<Guid, Dictionary<Guid, PCBasePermission>>();

        // PlayerID -> PCBaseID List
        // Contains PCBaseIDs player has access to enter.
        private Dictionary<Guid, List<Guid>> PCBaseIDsWithEnterPermissionByID { get; } = new Dictionary<Guid, List<Guid>>();

        protected override void OnCacheObjectSet(PCBasePermission entity)
        {
            SetEntityIntoDictionary(entity.PlayerID, entity.ID, entity, ByPlayerIDList);
            SetPCBaseIDsWithPermissionByID(entity);
        }

        protected override void OnCacheObjectRemoved(PCBasePermission entity)
        {
            RemoveEntityFromDictionary(entity.PlayerID, entity.ID, ByPlayerIDList);
            RemovePCBaseIDsWithPermissionByID(entity);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCBasePermission GetByID(Guid id)
        {
            return ByID[id];
        }

        private void SetPCBaseIDsWithPermissionByID(PCBasePermission entity)
        {
            if (!PCBaseIDsWithEnterPermissionByID.ContainsKey(entity.PlayerID))
            {
                PCBaseIDsWithEnterPermissionByID[entity.PlayerID] = new List<Guid>();
            }

            var list = PCBaseIDsWithEnterPermissionByID[entity.PlayerID];

            if (entity.CanEnterBuildings)
                list.Add(entity.PCBaseID);
            else list.Remove(entity.PCBaseID);
        }

        private void RemovePCBaseIDsWithPermissionByID(PCBasePermission entity)
        {
            if (!PCBaseIDsWithEnterPermissionByID.ContainsKey(entity.PlayerID))
            {
                PCBaseIDsWithEnterPermissionByID[entity.PlayerID] = new List<Guid>();
            }

            var list = PCBaseIDsWithEnterPermissionByID[entity.PlayerID];
            list.Remove(entity.PCBaseID);
        }

        public IEnumerable<PCBasePermission> GetAllByPlayerID(Guid id)
        {
            return ByPlayerIDList[id].Values;
        }

    }
}
