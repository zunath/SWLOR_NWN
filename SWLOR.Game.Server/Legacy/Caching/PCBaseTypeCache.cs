using SWLOR.Game.Server.Legacy.Data.Entity;

namespace SWLOR.Game.Server.Legacy.Caching
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
