using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class BuildingStyleCache: CacheBase<BuildingStyle>
    {
        // Note: This list is pretty small so we aren't storing any indexes.
        // LINQ should be pretty quick since the set is so small, but if performance
        // gets bad look into setting up a few lookup dictionaries. 

        public BuildingStyleCache() 
            : base("BuildingStyle")
        {
        }

        protected override void OnCacheObjectSet(BuildingStyle entity)
        {
        }

        protected override void OnCacheObjectRemoved(BuildingStyle entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public BuildingStyle GetByID(int id)
        {
            return ByID(id);
        }

        public BuildingStyle GetDefaultInteriorByBaseStructureID(int baseStructureID)
        {
            return (BuildingStyle)GetAll().Single(x => x.BaseStructureID == baseStructureID && x.IsDefault && x.BuildingTypeID == (int) Enumeration.BuildingType.Interior && x.IsActive).Clone();
        }

        public BuildingStyle GetDefaultExteriorByBaseStructureID(int baseStructureID)
        {
            return (BuildingStyle)GetAll().Single(x => x.BaseStructureID == baseStructureID && x.IsDefault && x.BuildingTypeID == (int)Enumeration.BuildingType.Exterior && x.IsActive).Clone();
        }

        public BuildingStyle GetByBaseStructureIDAndBuildingType(int baseStructureID, Enumeration.BuildingType buildingType)
        {
            return (BuildingStyle)GetAll().Single(x => x.BaseStructureID == baseStructureID && x.BuildingTypeID == (int)buildingType).Clone();
        }
    }
}
