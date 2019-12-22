using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class AuthorizedDMCache: CacheBase<AuthorizedDM>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, AuthorizedDM entity)
        {
            AddIndexToList("All", id);
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, AuthorizedDM entity)
        {
            RemoveIndexFromList("All", index);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public AuthorizedDM GetByID(int id)
        {
            return ByID(id);
        }

        public AuthorizedDM GetByCDKeyAndActiveOrDefault(string cdKey)
        {
            var list = GetIndexList("All");
            return (AuthorizedDM)list.SingleOrDefault(x => x.CDKey == cdKey && x.IsActive)?.Clone();
        }
    }
}
