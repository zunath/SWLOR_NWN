using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Event.SWLOR
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
