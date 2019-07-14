using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class AttributeCache: CacheBase<Attribute>
    {
        protected override void OnCacheObjectSet(Attribute entity)
        {
        }

        protected override void OnCacheObjectRemoved(Attribute entity)
        {
        }
    }
}
