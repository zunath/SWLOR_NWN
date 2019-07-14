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
    }
}
