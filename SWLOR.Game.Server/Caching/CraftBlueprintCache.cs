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

        protected override void OnSubscribeEvents()
        {
        }

        public CraftBlueprint GetByID(int id)
        {
            return ByID[id];
        }

        public CraftBlueprint GetByIDOrDefault(int id)
        {
            if (!ByID.ContainsKey(id))
                return default;
            return ByID[id];
        }
    }
}
