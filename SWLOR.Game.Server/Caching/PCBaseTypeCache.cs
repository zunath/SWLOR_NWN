using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCBaseTypeCache: CacheBase<PCBaseType>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, PCBaseType entity)
        {
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, PCBaseType entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCBaseType GetByID(int id)
        {
            return ByID(id);
        }
    }
}
