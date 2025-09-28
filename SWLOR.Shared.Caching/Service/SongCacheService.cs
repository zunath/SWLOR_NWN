using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Domain.World.ValueObjects;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Shared.Caching.Service
{
    /// <summary>
    /// Service for caching song data loaded from 2DA files
    /// </summary>
    public class SongCacheService : ISongCacheService
    {
        private readonly Dictionary<int, Song> _songs = new();
        private readonly Dictionary<int, Song> _playerBattleSongs = new();

        /// <summary>
        /// Loads all songs from the ambientmusic.2da file and caches them
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
        public void LoadSongList()
        {
            const string File = "ambientmusic";
            var rowCount = Get2DARowCount(File);

            for (var row = 0; row < rowCount; row++)
            {
                var description = Get2DAString(File, "Description", row);
                var displayName = Get2DAString(File, "DisplayName", row);
                var isAvailableAsBattleSong = Get2DAString(File, "PlayerBattleSong", row) == "1";

                // Skip record if a name cannot be determined.
                if (string.IsNullOrWhiteSpace(description) &&
                    string.IsNullOrWhiteSpace(displayName)) continue;

                string name = string.IsNullOrWhiteSpace(description) ?
                    displayName :
                    GetStringByStrRef(Convert.ToInt32(description));

                var song = new Song(row, name, isAvailableAsBattleSong);
                _songs[row] = song;

                if (isAvailableAsBattleSong)
                {
                    _playerBattleSongs[row] = song;
                }
            }
        }

        /// <summary>
        /// Retrieves all cached songs
        /// </summary>
        /// <returns>A list of all available songs</returns>
        public List<Song> GetAllSongs()
        {
            return _songs.Values.ToList();
        }

        /// <summary>
        /// Retrieves all battle songs that players can select
        /// </summary>
        /// <returns>A dictionary of battle songs by ID</returns>
        public Dictionary<int, Song> GetBattleSongs()
        {
            return _playerBattleSongs.ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Gets a specific song by ID
        /// </summary>
        /// <param name="songId">The ID of the song to retrieve</param>
        /// <returns>The song if found, null otherwise</returns>
        public Song GetSongById(int songId)
        {
            return _songs.TryGetValue(songId, out var song) ? song : null;
        }
    }
}
