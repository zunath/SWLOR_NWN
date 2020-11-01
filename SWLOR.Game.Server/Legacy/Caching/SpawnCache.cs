using SWLOR.Game.Server.Legacy.Data.Entity;

namespace SWLOR.Game.Server.Legacy.Caching
{
    public class SpawnCache: CacheBase<Spawn>
    {
        protected override void OnCacheObjectSet(Spawn entity)
        {
        }

        protected override void OnCacheObjectRemoved(Spawn entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public Spawn GetByID(int id)
        {
            return (Spawn)ByID[id].Clone();
        }
    }
}
