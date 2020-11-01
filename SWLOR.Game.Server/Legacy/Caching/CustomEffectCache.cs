namespace SWLOR.Game.Server.Legacy.Caching
{
    public class CustomEffectCache: CacheBase<Data.Entity.CustomEffect>
    {
        protected override void OnCacheObjectSet(Data.Entity.CustomEffect entity)
        {
        }

        protected override void OnCacheObjectRemoved(Data.Entity.CustomEffect entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public Data.Entity.CustomEffect GetByID(int id)
        {
            return (Data.Entity.CustomEffect)ByID[id].Clone();
        }
    }
}
