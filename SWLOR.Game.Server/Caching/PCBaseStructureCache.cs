using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Caching
{
    public class PCBaseStructureCache: CacheBase<PCBaseStructure>
    {
        private Dictionary<Guid, List<PCBaseStructure>> ByPCBaseID { get; } = new Dictionary<Guid, List<PCBaseStructure>>();
        private Dictionary<Guid, double> PowerInUseByPCBaseID { get; } = new Dictionary<Guid, double>();
        private Dictionary<Guid, double> CPUInUseByPCBaseID { get; } = new Dictionary<Guid, double>();

        protected override void OnCacheObjectSet(PCBaseStructure entity)
        {
            SetEntityIntoDictionary(entity.PCBaseID, entity, ByPCBaseID);
            RecalculatePowerAndCPU(entity);
        }

        protected override void OnCacheObjectRemoved(PCBaseStructure entity)
        {
            RemoveEntityFromDictionary(entity.PCBaseID, entity, ByPCBaseID);

            RecalculatePowerAndCPU(entity);
        }

        protected override void OnSubscribeEvents()
        {
        }

        private void RecalculatePowerAndCPU(PCBaseStructure entity)
        {
            double power = 0.0d;
            double cpu = 0.0d;
            var entities = ByPCBaseID[entity.PCBaseID];
            foreach (var structure in entities)
            {
                var baseStructureType = DataService.BaseStructure.GetByID(structure.BaseStructureID);
                if (baseStructureType.BaseStructureTypeID != (int) Enumeration.BaseStructureType.ControlTower)
                {
                    power += baseStructureType.Power;
                    cpu += baseStructureType.CPU;
                }
            }

            PowerInUseByPCBaseID[entity.PCBaseID] = power;
            CPUInUseByPCBaseID[entity.PCBaseID] = cpu;
        }

        public PCBaseStructure GetByID(Guid id)
        {
            return ByID[id];
        }

        public IEnumerable<PCBaseStructure> GetAllByPCBaseID(Guid pcBaseID)
        {
            if(!ByPCBaseID.ContainsKey(pcBaseID))
                ByPCBaseID[pcBaseID] = new List<PCBaseStructure>();
            return ByPCBaseID[pcBaseID];
        }

        public double GetPowerInUseByPCBaseID(Guid pcBaseID)
        {
            return PowerInUseByPCBaseID[pcBaseID];
        }

        public double GetCPUInUseByPCBaseID(Guid pcBaseID)
        {
            return CPUInUseByPCBaseID[pcBaseID];
        }
    }
}
