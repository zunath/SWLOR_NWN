using System.Collections.Generic;
using SWLOR.Game.Server.Service.MusicService;

namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>
    /// Which song a musician plays: the one they picked in the Perform window when it is a Cantina song they own,
    /// otherwise the first Cantina song in their collection, or the starter Cantina song every player owns.
    /// </summary>
    public static class PerformanceSongs
    {
        /// <summary>The preference value that leaves the choice to the first collected Cantina song.</summary>
        public const int AutomaticSong = -1;

        /// <summary>The Cantina songs the player owns, plus the starter song every player owns, in catalog order.</summary>
        public static IReadOnlyList<PerformanceSongOption> GetAvailableSongs(string playerId)
        {
            var collected = Music.GetCollectedTrackIds(playerId);
            var songs = new List<PerformanceSongOption>();
            foreach (var song in Music.GetCollectibleSongs())
            {
                if (song.Category != MusicCategoryType.Cantina)
                    continue;

                if (collected.Contains(song.TrackId) || song.Acquisition == MusicAcquisitionType.Starter)
                    songs.Add(new PerformanceSongOption(song.TrackId, Music.GetTrackName(song.TrackId)));
            }

            return songs;
        }

        public static int ChooseSong(string playerId, int preferredTrackId = AutomaticSong)
        {
            var collected = Music.GetCollectedTrackIds(playerId);
            var firstCollected = -1;
            var starter = -1;
            var preferredIsAvailable = false;
            foreach (var song in Music.GetCollectibleSongs())
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
