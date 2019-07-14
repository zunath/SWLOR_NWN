using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCCraftedBlueprintCache: CacheBase<PCCraftedBlueprint>
    {
        protected override void OnCacheObjectSet(PCCraftedBlueprint entity)
        {
        }

        protected override void OnCacheObjectRemoved(PCCraftedBlueprint entity)
        {
        }
    }
}
