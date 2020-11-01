using SWLOR.Game.Server.Legacy.Data.Entity;

namespace SWLOR.Game.Server.Legacy.Caching
{
    public class EnmityAdjustmentRuleCache: CacheBase<EnmityAdjustmentRule>
    {
        protected override void OnCacheObjectSet(EnmityAdjustmentRule entity)
        {
        }

        protected override void OnCacheObjectRemoved(EnmityAdjustmentRule entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public EnmityAdjustmentRule GetByID(int id)
        {
            return (EnmityAdjustmentRule)ByID[id].Clone();
        }
    }
}
