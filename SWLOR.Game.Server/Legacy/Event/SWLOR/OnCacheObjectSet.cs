using SWLOR.Game.Server.Legacy.Data.Contracts;

namespace SWLOR.Game.Server.Legacy.Event.SWLOR
{
    public class OnCacheObjectSet<T>
        where T: IEntity
    {
        public T Entity { get; set; }

        public OnCacheObjectSet(T entity)
        {
            Entity = entity;
        }
    }
}
