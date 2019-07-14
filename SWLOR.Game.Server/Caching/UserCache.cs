using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class UserCache: CacheBase<User>
    {
        protected override void OnCacheObjectSet(User entity)
        {
        }

        protected override void OnCacheObjectRemoved(User entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public User GetByID(int id)
        {
            return ByID[id];
        }
    }
}
