using System.Linq;
using SWLOR.Game.Server.Legacy.Data.Entity;
using BuildingType = SWLOR.Game.Server.Legacy.Enumeration.BuildingType;

namespace SWLOR.Game.Server.Legacy.Caching
{
    public class BuildingStyleCache: CacheBase<BuildingStyle>
    {
        // Note: This list is pretty small so we aren't storing any indexes.
        // LINQ should be pretty quick since the set is so small, but if performance
        // gets bad look into setting up a few lookup dictionaries. 

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
            return (BuildingStyle)ByID[id].Clone();
        }

        public BuildingStyle GetDefaultInteriorByBaseStructureID(int baseStructureID)
        {
            return (BuildingStyle)All.Single(x => x.BaseStructureID == baseStructureID && x.IsDefault && x.BuildingTypeID == (int) BuildingType.Interior && x.IsActive).Clone();
        }

        public BuildingStyle GetDefaultExteriorByBaseStructureID(int baseStructureID)
        {
            return (BuildingStyle)All.Single(x => x.BaseStructureID == baseStructureID && x.IsDefault && x.BuildingTypeID == (int)BuildingType.Exterior && x.IsActive).Clone();
        }

        public BuildingStyle GetByBaseStructureIDAndBuildingType(int baseStructureID, BuildingType buildingType)
        {
            return (BuildingStyle)All.Single(x => x.BaseStructureID == baseStructureID && x.BuildingTypeID == (int)buildingType).Clone();
        }
    }
}
