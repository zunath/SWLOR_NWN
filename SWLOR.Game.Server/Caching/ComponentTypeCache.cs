using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class ComponentTypeCache: CacheBase<ComponentType>
    {
        protected override void OnCacheObjectSet(ComponentType entity)
        {
        }

        protected override void OnCacheObjectRemoved(ComponentType entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public ComponentType GetByID(int id)
        {
            return ByID[id];
        }
    }
}
