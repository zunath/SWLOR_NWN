using SWLOR.Game.Server.Legacy.Data.Entity;

namespace SWLOR.Game.Server.Legacy.Caching
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
