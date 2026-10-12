using SWLOR.Game.Server.Service.KeyItemService;

namespace SWLOR.Game.Server.Service.MusicService
{
    /// <summary>
    /// A collectible song. The key item tracks ownership; the track ID is its ambientmusic.2da row.
    /// </summary>
    public class MusicSong
    {
        public KeyItemType KeyItem { get; }
        public int TrackId { get; }
        public MusicCategoryType Category { get; }
        public MusicAcquisitionType Acquisition { get; }

        public MusicSong(KeyItemType keyItem, int trackId, MusicCategoryType category, MusicAcquisitionType acquisition)
        {
            KeyItem = keyItem;
            TrackId = trackId;
            Category = category;
            Acquisition = acquisition;
        }
    }
}
