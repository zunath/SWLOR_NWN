using SWLOR.Game.Server.Legacy.Data.Entity;

namespace SWLOR.Game.Server.Legacy.Caching
{
    public class DMActionTypeCache: CacheBase<DMActionType>
    {
        protected override void OnCacheObjectSet(DMActionType entity)
        {
        }

        protected override void OnCacheObjectRemoved(DMActionType entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public DMActionType GetByID(int id)
        {
            return (DMActionType)ByID[id].Clone();
        }
    }
}
