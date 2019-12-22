using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class JukeboxSongCache: CacheBase<JukeboxSong>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, JukeboxSong entity)
        {
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, JukeboxSong entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public JukeboxSong GetByID(int id)
        {
            return ByID(id);
        }
    }
}
