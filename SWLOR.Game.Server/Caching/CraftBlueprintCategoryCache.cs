using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class CraftBlueprintCategoryCache: CacheBase<CraftBlueprintCategory>
    {
        protected override void OnCacheObjectSet(CraftBlueprintCategory entity)
        {
        }

        protected override void OnCacheObjectRemoved(CraftBlueprintCategory entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public CraftBlueprintCategory GetByID(int id)
        {
            return ByID[id];
        }
    }
}
