using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class AttributeCache: CacheBase<Attribute>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, Attribute entity)
        {
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, Attribute entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public Attribute GetByID(int id)
        {
            return ByID(id);
        }
    }
}
