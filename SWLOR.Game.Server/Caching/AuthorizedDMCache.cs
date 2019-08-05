using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class AuthorizedDMCache: CacheBase<AuthorizedDM>
    {
        protected override void OnCacheObjectSet(AuthorizedDM entity)
        {
        }

        protected override void OnCacheObjectRemoved(AuthorizedDM entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public AuthorizedDM GetByID(int id)
        {
            return (AuthorizedDM)ByID[id].Clone();
        }

        public AuthorizedDM GetByCDKeyAndActiveOrDefault(string cdKey)
        {
            return (AuthorizedDM)ByID.Values.SingleOrDefault(x => x.CDKey == cdKey && x.IsActive)?.Clone();
        }
    }
}
