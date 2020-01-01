using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCBaseStructurePermissionCache: CacheBase<PCBaseStructurePermission>
    {
        public PCBaseStructurePermissionCache() 
            : base("PCBaseStructurePermission")
        {
        }

        private const string ByPCBaseStructureIDIndex = "ByPCBaseStructureID";
        private const string ByPlayerIDIndex = "ByPlayerID";

        
        protected override void OnCacheObjectSet(PCBaseStructurePermission entity)
        {
            SetIntoListIndex(ByPCBaseStructureIDIndex, entity.PCBaseStructureID.ToString(), entity);
            SetIntoListIndex(ByPlayerIDIndex, entity.PlayerID.ToString(), entity);
        }

        protected override void OnCacheObjectRemoved(PCBaseStructurePermission entity)
        {
            RemoveFromListIndex(ByPCBaseStructureIDIndex, entity.PCBaseStructureID.ToString(), entity);
            RemoveFromListIndex(ByPlayerIDIndex, entity.PlayerID.ToString(), entity);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCBaseStructurePermission GetByID(Guid id)
        {
            return ByID(id);
        }

        public IEnumerable<PCBaseStructurePermission> GetAllByPlayerID(Guid playerID)
        {
            if (!ExistsByListIndex(ByPlayerIDIndex, playerID.ToString()))
                return new List<PCBaseStructurePermission>();

            return GetFromListIndex(ByPlayerIDIndex, playerID.ToString());
        }

        public PCBaseStructurePermission GetPublicPermissionOrDefault(Guid pcBaseStructureID)
        {
            return GetAll().SingleOrDefault(x => x.PCBaseStructureID == pcBaseStructureID && x.IsPublicPermission);
        }

        public PCBaseStructurePermission GetPlayerPrivatePermissionOrDefault(Guid playerID, Guid pcBaseStructureID)
        {
            if (!ExistsByListIndex(ByPlayerIDIndex, playerID.ToString()))
                return default;

            var permissions = GetFromListIndex(ByPlayerIDIndex, playerID.ToString());
            return permissions.SingleOrDefault(x => !x.IsPublicPermission && x.PCBaseStructureID == pcBaseStructureID);
        }

        public IEnumerable<PCBaseStructurePermission> GetAllByPCBaseStructureID(Guid pcBaseStructureID)
        {
            if (!ExistsByListIndex(ByPCBaseStructureIDIndex, pcBaseStructureID.ToString()))
            {
                return new List<PCBaseStructurePermission>();
            }

            return GetFromListIndex(ByPCBaseStructureIDIndex, pcBaseStructureID.ToString());
        }
    }
}
