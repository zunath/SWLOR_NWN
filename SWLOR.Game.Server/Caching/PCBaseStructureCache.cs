using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Caching
{
    public class PCBaseStructureCache: CacheBase<PCBaseStructure>
    {
        private Dictionary<Guid, Dictionary<Guid, PCBaseStructure>> ByPCBaseID { get; } = new Dictionary<Guid, Dictionary<Guid, PCBaseStructure>>();
        private Dictionary<Guid, double> PowerInUseByPCBaseID { get; } = new Dictionary<Guid, double>();
        private Dictionary<Guid, double> CPUInUseByPCBaseID { get; } = new Dictionary<Guid, double>();
        private Dictionary<Guid, Dictionary<Guid, PCBaseStructure>> ByParentPCBaseStructureID { get; } = new Dictionary<Guid, Dictionary<Guid, PCBaseStructure>>();

        protected override void OnCacheObjectSet(PCBaseStructure entity)
        {
            SetEntityIntoDictionary(entity.PCBaseID, entity.ID, entity, ByPCBaseID);
            RecalculatePowerAndCPU(entity);
            if (entity.ParentPCBaseStructureID != null)
            {
                SetEntityIntoDictionary((Guid)entity.ParentPCBaseStructureID, entity.ID, entity, ByParentPCBaseStructureID);
            }
        }

        protected override void OnCacheObjectRemoved(PCBaseStructure entity)
        {
            RemoveEntityFromDictionary(entity.PCBaseID, entity.ID, ByPCBaseID);
            RecalculatePowerAndCPU(entity);
            if (entity.ParentPCBaseStructureID != null)
            {
                RemoveEntityFromDictionary((Guid)entity.ParentPCBaseStructureID, entity.ID, ByParentPCBaseStructureID);
            }
        }

        protected override void OnSubscribeEvents()
        {
        }

        private void RecalculatePowerAndCPU(PCBaseStructure entity)
        {
            double power = 0.0d;
            double cpu = 0.0d;
            var entities = ByPCBaseID[entity.PCBaseID];
            foreach (var structure in entities.Values)
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
            return (PCBaseStructure)ByID[id].Clone();
        }

        public PCBaseStructure GetByIDOrDefault(Guid id)
        {
            if (!ByID.ContainsKey(id))
                return default;
            return (PCBaseStructure)ByID[id].Clone();
        }

        public IEnumerable<PCBaseStructure> GetAllByPCBaseID(Guid pcBaseID)
        {
            if(!ByPCBaseID.ContainsKey(pcBaseID))
                return new List<PCBaseStructure>();

            var list = new List<PCBaseStructure>();
            foreach (var record in ByPCBaseID[pcBaseID].Values)
            {
                list.Add((PCBaseStructure)record.Clone());
            }
            return list;
        }

        public double GetPowerInUseByPCBaseID(Guid pcBaseID)
        {
            if (!PowerInUseByPCBaseID.ContainsKey(pcBaseID)) return 0.0d;

            return PowerInUseByPCBaseID[pcBaseID];
        }

        public double GetCPUInUseByPCBaseID(Guid pcBaseID)
        {
            if (!CPUInUseByPCBaseID.ContainsKey(pcBaseID)) return 0.0d;

            return CPUInUseByPCBaseID[pcBaseID];
        }

        public PCBaseStructure GetStarshipInteriorByPCBaseIDOrDefault(Guid pcBaseID)
        {
            return (PCBaseStructure)ByPCBaseID[pcBaseID].Values.SingleOrDefault(x => x.InteriorStyleID != null)?.Clone();
        }

        public PCBaseStructure GetStarshipExteriorByPCBaseID(Guid pcBaseID)
        {
            return (PCBaseStructure)ByPCBaseID[pcBaseID].Values.SingleOrDefault(x => x.ExteriorStyleID > 0)?.Clone();
        }

        public IEnumerable<PCBaseStructure> GetAllByParentPCBaseStructureID(Guid parentPCBaseStructureID)
        {
            if(!ByParentPCBaseStructureID.ContainsKey(parentPCBaseStructureID))
                return new List<PCBaseStructure>();

            var list = new List<PCBaseStructure>();
            foreach (var record in ByParentPCBaseStructureID[parentPCBaseStructureID].Values)
            {
                list.Add((PCBaseStructure)record.Clone());
            }

            return list;
        }
    }
}
