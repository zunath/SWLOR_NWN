using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Composition
{
    /// <summary>
    /// Gives converted services the static <c>DB</c> class through <see cref="IDBService"/>. Every member forwards to
    /// the same <c>DB</c> member with the same arguments. Temporary: it is deleted when <c>DB</c> is converted.
    /// </summary>
    internal sealed class StaticDBAdapter : IDBService
    {
        public T Get<T>(string id)
            where T : EntityBase
        {
            return DB.Get<T>(id);
        }

        public void Set<T>(T entity)
            where T : EntityBase
        {
            DB.Set(entity);
        }
    }
}
