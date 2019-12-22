using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class CraftBlueprintCache: CacheBase<CraftBlueprint>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, CraftBlueprint entity)
        {
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, CraftBlueprint entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public CraftBlueprint GetByID(int id)
        {
            return ByID(id);
        }

        public CraftBlueprint GetByIDOrDefault(int id)
        {
            if (!Exists(id))
                return default;
            return ByID(id);
        }
    }
}
