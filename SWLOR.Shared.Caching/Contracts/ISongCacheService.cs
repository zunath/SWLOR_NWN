using SWLOR.Shared.Domain.World.ValueObjects;

namespace SWLOR.Shared.Caching.Contracts
{
    /// <summary>
    /// Interface for caching song data loaded from 2DA files
    /// </summary>
    public interface ISongCacheService
    {
        /// <summary>
        /// Loads all songs from the ambientmusic.2da file and caches them
        /// </summary>
        void LoadCache();

        /// <summary>
        /// Retrieves all cached songs
        /// </summary>
        /// <returns>A list of all available songs</returns>
        List<Song> GetAllSongs();

        /// <summary>
        /// Retrieves all battle songs that players can select
        /// </summary>
        /// <returns>A dictionary of battle songs by ID</returns>
        Dictionary<int, Song> GetBattleSongs();

        /// <summary>
        /// Gets a specific song by ID
        /// </summary>
        /// <param name="songId">The ID of the song to retrieve</param>
        /// <returns>The song if found, null otherwise</returns>
        Song GetSongById(int songId);
    }
}
