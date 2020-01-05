using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.NWNX;
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
        private const string PowerInUseByPCBaseIDIndex = "PowerInUseByPCBaseID";
        private const string CPUInUseByPCBaseIDIndex = "CPUInUseByPCBaseID";
        private const string ByParentPCBaseStructureIDIndex = "ByParentPCBaseStructureID";

        protected override void OnCacheObjectSet(PCBaseStructure entity)
        {
            SetIntoListIndex(ByPCBaseIDIndex, entity.PCBaseID.ToString(), entity);
            RecalculatePowerAndCPU(entity);
            if (entity.ParentPCBaseStructureID != null)
            {
                SetIntoListIndex(ByParentPCBaseStructureIDIndex, entity.ParentPCBaseStructureID.ToString(), entity);
            }
        }

        protected override void OnCacheObjectRemoved(PCBaseStructure entity)
        {
            RemoveFromListIndex(ByPCBaseIDIndex, entity.PCBaseID.ToString(), entity);
            RecalculatePowerAndCPU(entity);
            if (entity.ParentPCBaseStructureID != null)
            {
                RemoveFromListIndex(ByParentPCBaseStructureIDIndex, entity.ParentPCBaseStructureID.ToString(), entity);
            }
        }

        protected override void OnSubscribeEvents()
        {
        }

        private void RecalculatePowerAndCPU(PCBaseStructure entity)
        {
            double power = 0.0d;
            double cpu = 0.0d;
            var entities = GetFromListIndex(ByPCBaseIDIndex, entity.PCBaseID.ToString());
            foreach (var structure in entities)
            {
                var baseStructureType = BaseService.GetBaseStructure(structure.BaseStructureID);
                if (baseStructureType.BaseStructureType != BaseStructureType.ControlTower)
                {
                    power += baseStructureType.Power;
                    cpu += baseStructureType.CPU;
                }
            }

            NWNXRedis.Set(PowerInUseByPCBaseIDIndex + ":" + entity.PCBaseID, JsonConvert.SerializeObject(power));
            NWNXRedis.Set(CPUInUseByPCBaseIDIndex + ":" + entity.PCBaseID, JsonConvert.SerializeObject(cpu));
        }

        public PCBaseStructure GetByID(Guid id)
        {
            return ByID(id);
        }

        public PCBaseStructure GetByIDOrDefault(Guid id)
        {
            if (!Exists(id))
                return default;
            return ByID(id);
        }

        public IEnumerable<PCBaseStructure> GetAllByPCBaseID(Guid pcBaseID)
        {
            if(!ExistsByListIndex(ByPCBaseIDIndex, pcBaseID.ToString()))
                return new List<PCBaseStructure>();

            return GetFromListIndex(ByPCBaseIDIndex, pcBaseID.ToString());
        }

        public double GetPowerInUseByPCBaseID(Guid pcBaseID)
        {
            var key = PowerInUseByPCBaseIDIndex + ":" + pcBaseID;
            if (!NWNXRedis.Exists(key)) return 0.0d;

            return JsonConvert.DeserializeObject<double>(NWNXRedis.Get(key));
        }

        public double GetCPUInUseByPCBaseID(Guid pcBaseID)
        {
            var key = CPUInUseByPCBaseIDIndex + ":" + pcBaseID;
            if (!NWNXRedis.Exists(key)) return 0.0d;

            return JsonConvert.DeserializeObject<double>(NWNXRedis.Get(key));
        }

        public PCBaseStructure GetStarshipInteriorByPCBaseIDOrDefault(Guid pcBaseID)
        {
            if (!ExistsByListIndex(ByPCBaseIDIndex, pcBaseID.ToString()))
                return default;

            return GetFromListIndex(ByPCBaseIDIndex, pcBaseID.ToString())
                .SingleOrDefault(x => x.InteriorStyleID != null);
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
    }
}
