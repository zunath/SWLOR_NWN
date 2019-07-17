using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCBaseTypeCache: CacheBase<PCBaseType>
    {
        protected override void OnCacheObjectSet(PCBaseType entity)
        {
        }

        protected override void OnCacheObjectRemoved(PCBaseType entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCBaseType GetByID(int id)
        {
            return (PCBaseType)ByID[id].Clone();
        }
    }
}
