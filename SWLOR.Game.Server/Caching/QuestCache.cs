using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class QuestCache: CacheBase<Quest>
    {
        protected override void OnCacheObjectSet(Quest entity)
        {
        }

        protected override void OnCacheObjectRemoved(Quest entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public Quest GetByID(int id)
        {
            return (Quest)ByID[id].Clone();
        }

        public bool ExistsByID(int id)
        {
            return ByID.ContainsKey(id);
        }
    }
}
