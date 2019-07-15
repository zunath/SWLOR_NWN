using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class BuildingStyleCache: CacheBase<BuildingStyle>
    {
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
            return ByID[id];
        }

        public BuildingStyle GetDefaultInteriorByBaseStructureID(int baseStructureID)
        {
            return All.Single(x => x.BaseStructureID == baseStructureID && x.IsDefault && x.BuildingTypeID == (int) Enumeration.BuildingType.Interior && x.IsActive);
        }

        public BuildingStyle GetDefaultExteriorByBaseStructureID(int baseStructureID)
        {
            return All.Single(x => x.BaseStructureID == baseStructureID && x.IsDefault && x.BuildingTypeID == (int)Enumeration.BuildingType.Exterior && x.IsActive);
        }
    }
}
