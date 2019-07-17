using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class JukeboxSongCache: CacheBase<JukeboxSong>
    {
        protected override void OnCacheObjectSet(JukeboxSong entity)
        {
        }

        protected override void OnCacheObjectRemoved(JukeboxSong entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public JukeboxSong GetByID(int id)
        {
            return (JukeboxSong)ByID[id].Clone();
        }
    }
}
