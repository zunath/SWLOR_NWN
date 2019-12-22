using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PerkCache: CacheBase<Data.Entity.Perk>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, Data.Entity.Perk entity)
        {
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, Data.Entity.Perk entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public Data.Entity.Perk GetByID(int id)
        {
            return ByID(id);
        }

        public Data.Entity.Perk GetByIDOrDefault(int id)
        {
            if (!Exists(id))
                return default;
            return ByID(id);
        }
    }
}
