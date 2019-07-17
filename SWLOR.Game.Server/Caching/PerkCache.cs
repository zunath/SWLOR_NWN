using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PerkCache: CacheBase<Data.Entity.Perk>
    {
        protected override void OnCacheObjectSet(Data.Entity.Perk entity)
        {
        }

        protected override void OnCacheObjectRemoved(Data.Entity.Perk entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public Data.Entity.Perk GetByID(int id)
        {
            return (Data.Entity.Perk)ByID[id].Clone();
        }

        public Data.Entity.Perk GetByIDOrDefault(int id)
        {
            if (!ByID.ContainsKey(id))
                return default;
            return (Data.Entity.Perk)ByID[id].Clone();
        }
    }
}
