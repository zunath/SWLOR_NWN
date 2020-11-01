using SWLOR.Game.Server.Legacy.Data.Entity;

namespace SWLOR.Game.Server.Legacy.Caching
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
            return (CraftBlueprint)ByID[id].Clone();
        }

        public CraftBlueprint GetByIDOrDefault(int id)
        {
            if (!ByID.ContainsKey(id))
                return default(CraftBlueprint);
            return (CraftBlueprint)ByID[id].Clone();
        }
    }
}
