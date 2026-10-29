using SWLOR.Game.Server.Service.MusicService;

namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>
    /// Which song a musician plays. Until the performance window lets musicians pick, it is the first Cantina song
    /// in their collection, or the starter Cantina song every player owns.
    /// </summary>
    public static class PerformanceSongs
    {
        public static int ChooseSong(string playerId)
        {
            var collected = Music.GetCollectedTrackIds(playerId);
            var starter = -1;
            foreach (var song in Music.GetCollectibleSongs())
            {
                if (song.Category != MusicCategoryType.Cantina)
                    continue;

                if (collected.Contains(song.TrackId))
                    return song.TrackId;

                if (starter < 0 && song.Acquisition == MusicAcquisitionType.Starter)
                    starter = song.TrackId;
            }

            return starter;
        }
    }
}
