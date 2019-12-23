using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCBasePermissionCache: CacheBase<PCBasePermission>
    {
        public PCBasePermissionCache() 
            : base("PCBasePermission")
        {
        }

        private const string ByPlayerIDIndex = "ByPlayerID";
        private const string ByPCBaseIDPrivateIndex = "ByPCBaseID";
        private const string ByPCBaseIDAllIndex = "ByPCBaseIDAll";

        protected override void OnCacheObjectSet(PCBasePermission entity)
        {
            SetIntoListIndex(ByPlayerIDIndex, entity.PlayerID.ToString(), entity);

            if (!entity.IsPublicPermission)
                SetIntoListIndex(ByPCBaseIDPrivateIndex, entity.PCBaseID.ToString(), entity);

            SetIntoListIndex(ByPCBaseIDAllIndex, entity.PCBaseID.ToString(), entity);
        }

        protected override void OnCacheObjectRemoved(PCBasePermission entity)
        {
            RemoveFromListIndex(ByPlayerIDIndex, entity.PlayerID.ToString(), entity);

            if (!entity.IsPublicPermission)
                RemoveFromListIndex(ByPCBaseIDPrivateIndex, entity.PCBaseID.ToString(), entity);

            RemoveFromListIndex(ByPCBaseIDAllIndex, entity.PCBaseID.ToString(), entity);
        }

        protected override void OnSubscribeEvents()
        {
        }

        /// <summary>
        /// Returns a PCBasePermission by its unique ID.
        /// Throws KeyNotFound exception if ID doesn't exist.
        /// </summary>
        /// <param name="id">The unique ID to retrieve</param>
        /// <returns></returns>
        public PCBasePermission GetByID(Guid id)
        {
            return ByID(id);
        }

        public IEnumerable<PCBasePermission> GetAllByPlayerID(Guid id)
        {
            if (!ExistsByListIndex(ByPlayerIDIndex, id.ToString()))
                return new List<PCBasePermission>();

            return GetFromListIndex(ByPlayerIDIndex, id.ToString());
        }

        public PCBasePermission GetByPlayerAndPCBaseIDOrDefault(Guid playerID, Guid pcBaseID)
        {
            if (!ExistsByListIndex(ByPlayerIDIndex, playerID.ToString()))
                return default;

            return GetFromListIndex(ByPlayerIDIndex, playerID.ToString())
                .FirstOrDefault(x => x.PCBaseID == pcBaseID);
        }

        public PCBasePermission GetPublicPermissionOrDefault(Guid pcBaseID)
        {
            return GetAll().SingleOrDefault(x => x.PCBaseID == pcBaseID && x.IsPublicPermission);
        }

        public PCBasePermission GetPlayerPrivatePermissionOrDefault(Guid playerID, Guid pcBaseID)
        {
            if (!ExistsByListIndex(ByPlayerIDIndex, playerID.ToString()))
                return default;

            var permissions = GetFromListIndex(ByPlayerIDIndex, playerID.ToString());
            return permissions.SingleOrDefault(x => !x.IsPublicPermission && x.PCBaseID == pcBaseID);
        }

        public IEnumerable<PCBasePermission> GetAllByHasPrivatePermissionToBase(Guid pcBaseID)
        {
            if (!ExistsByListIndex(ByPCBaseIDPrivateIndex, pcBaseID.ToString()))
                return new List<PCBasePermission>();

            return GetFromListIndex(ByPCBaseIDPrivateIndex, pcBaseID.ToString());
        }

        public IEnumerable<PCBasePermission> GetAllPermissionsByPCBaseID(Guid pcBaseID)
        {
            if (!ExistsByListIndex(ByPCBaseIDAllIndex, pcBaseID.ToString()))
                return new List<PCBasePermission>();

            return GetFromListIndex(ByPCBaseIDAllIndex, pcBaseID.ToString());
        }
        
    }
}
