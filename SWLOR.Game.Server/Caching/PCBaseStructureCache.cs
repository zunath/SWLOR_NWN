using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Caching
{
    public class PCBaseStructureCache: CacheBase<PCBaseStructure>
    {
        public PCBaseStructureCache() 
            : base("PCBaseStructure")
        {
        }

        private const string ByPCBaseIDIndex = "ByPCBaseID";
        private const string ByParentPCBaseStructureIDIndex = "ByParentPCBaseStructureID";
        private const string ByBaseStructureTypeIndex = "ByBaseStructureType";

        protected override void OnCacheObjectSet(PCBaseStructure entity)
        {
            SetIntoListIndex(ByPCBaseIDIndex, entity.PCBaseID.ToString(), entity);
            if (entity.ParentPCBaseStructureID != null)
            {
                SetIntoListIndex(ByParentPCBaseStructureIDIndex, entity.ParentPCBaseStructureID.ToString(), entity);
            }

            var baseStructure = BaseService.GetBaseStructure(entity.BaseStructureID);
            SetIntoListIndex(ByBaseStructureTypeIndex, $"{baseStructure.BaseStructureType}", entity);
        }

        protected override void OnCacheObjectRemoved(PCBaseStructure entity)
        {
            RemoveFromListIndex(ByPCBaseIDIndex, entity.PCBaseID.ToString(), entity);
            if (entity.ParentPCBaseStructureID != null)
            {
                RemoveFromListIndex(ByParentPCBaseStructureIDIndex, entity.ParentPCBaseStructureID.ToString(), entity);
            }

            var baseStructure = BaseService.GetBaseStructure(entity.BaseStructureID);
            RemoveFromListIndex(ByBaseStructureTypeIndex, $"{baseStructure.BaseStructureType}", entity);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCBaseStructure GetByID(Guid id)
        {
            return ByID(id.ToString());
        }

        public PCBaseStructure GetByIDOrDefault(Guid id)
        {
            if (!Exists(id))
                return default;
            return ByID(id.ToString());
        }

        public IEnumerable<PCBaseStructure> GetAllByPCBaseID(Guid pcBaseID)
        {
            if(!ExistsByListIndex(ByPCBaseIDIndex, pcBaseID.ToString()))
                return new List<PCBaseStructure>();

            return GetFromListIndex(ByPCBaseIDIndex, pcBaseID.ToString());
        }

        public PCBaseStructure GetStarshipInteriorByPCBaseIDOrDefault(Guid pcBaseID)
        {
            if (!ExistsByListIndex(ByPCBaseIDIndex, pcBaseID.ToString()))
                return default;

            return GetFromListIndex(ByPCBaseIDIndex, pcBaseID.ToString())
                .SingleOrDefault(x => x.InteriorStyleID != BuildingStyle.Invalid);
        }

        public PCBaseStructure GetStarshipExteriorByPCBaseID(Guid pcBaseID)
        {
            if (!ExistsByListIndex(ByPCBaseIDIndex, pcBaseID.ToString()))
                return default;

            return GetFromListIndex(ByPCBaseIDIndex, pcBaseID.ToString())
                .SingleOrDefault(x => x.ExteriorStyleID > 0);
        }

        public IEnumerable<PCBaseStructure> GetAllByParentPCBaseStructureID(Guid parentPCBaseStructureID)
        {
            if(!ExistsByListIndex(ByParentPCBaseStructureIDIndex, parentPCBaseStructureID.ToString()))
                return new List<PCBaseStructure>();

            return GetFromListIndex(ByParentPCBaseStructureIDIndex, parentPCBaseStructureID.ToString());
        }

        public IEnumerable<PCBaseStructure> GetAllByBaseStructureTypeID(BaseStructureType type)
        {
            if(!ExistsByListIndex(ByBaseStructureTypeIndex, $"{type}"))
                return new List<PCBaseStructure>();

            return GetFromListIndex(ByBaseStructureTypeIndex, $"{type}");
        }
    }
}
