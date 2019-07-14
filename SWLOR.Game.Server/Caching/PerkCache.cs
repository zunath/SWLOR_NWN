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

        public Data.Entity.Perk GetByID(int id)
        {
            return ByID[id];
        }
    }
}
