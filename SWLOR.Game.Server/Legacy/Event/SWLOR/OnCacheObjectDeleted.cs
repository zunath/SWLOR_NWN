namespace SWLOR.Game.Server.Legacy.Event.SWLOR
{
    public class OnCacheObjectDeleted<T>
    {
        public T Entity { get; set; }

        public OnCacheObjectDeleted(T entity)
        {
            Entity = entity;
        }
    }

}
