using System.Collections.Generic;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Service.MusicService;
using SWLOR.Game.Server.Service.PerformanceService;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// Which song a musician plays: the logic that used to be static in <c>Service.PerformanceService.PerformanceSongs</c>,
    /// moved here unchanged. It reads the player's collection from <see cref="IMusicService"/>.
    /// </summary>
    public class PerformanceSongService : IPerformanceSongService
    {
        private readonly IMusicService _music;

        public PerformanceSongService(IMusicService music)
        {
            _music = music;
        }

        public IReadOnlyList<PerformanceSongOption> GetAvailableSongs(string playerId)
        {
            var collected = _music.GetCollectedTrackIds(playerId);
            var songs = new List<PerformanceSongOption>();
            foreach (var song in _music.GetCollectibleSongs())
            {
                if (song.Category != MusicCategoryType.Cantina)
                    continue;

                if (collected.Contains(song.TrackId) || song.Acquisition == MusicAcquisitionType.Starter)
                    songs.Add(new PerformanceSongOption(song.TrackId, _music.GetTrackName(song.TrackId)));
            }

            return songs;
        }

        public int ChooseSong(string playerId, int preferredTrackId = PerformanceSongs.AutomaticSong)
        {
            var collected = _music.GetCollectedTrackIds(playerId);
            var firstCollected = -1;
            var starter = -1;
            var preferredIsAvailable = false;
            foreach (var song in _music.GetCollectibleSongs())
            {
                if (song.Category != MusicCategoryType.Cantina)
                    continue;

                var owned = collected.Contains(song.TrackId);
                if (preferredTrackId >= 0 &&
                    song.TrackId == preferredTrackId &&
                    (owned || song.Acquisition == MusicAcquisitionType.Starter))
                {
                    preferredIsAvailable = true;
                }

                if (owned && firstCollected < 0)
                    firstCollected = song.TrackId;

                if (starter < 0 && song.Acquisition == MusicAcquisitionType.Starter)
                    starter = song.TrackId;
            }

            if (preferredIsAvailable)
                return preferredTrackId;

            return firstCollected >= 0 ? firstCollected : starter;
        }
    }
}
