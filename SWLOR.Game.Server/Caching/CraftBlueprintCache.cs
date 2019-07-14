using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class CraftBlueprintCache: CacheBase<CraftBlueprint>
    {
        protected override void OnCacheObjectSet(CraftBlueprint entity)
        {
        }

        protected override void OnCacheObjectRemoved(CraftBlueprint entity)
        {
        }
    }
}
