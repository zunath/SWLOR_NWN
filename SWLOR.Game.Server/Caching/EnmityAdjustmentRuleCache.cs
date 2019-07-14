using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class EnmityAdjustmentRuleCache: CacheBase<EnmityAdjustmentRule>
    {
        protected override void OnCacheObjectSet(EnmityAdjustmentRule entity)
        {
        }

        protected override void OnCacheObjectRemoved(EnmityAdjustmentRule entity)
        {
        }
    }
}
