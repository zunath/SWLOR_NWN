using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class EnmityAdjustmentRuleCache: CacheBase<EnmityAdjustmentRule>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, EnmityAdjustmentRule entity)
        {
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, EnmityAdjustmentRule entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public EnmityAdjustmentRule GetByID(int id)
        {
            return ByID(id);
        }
    }
}
