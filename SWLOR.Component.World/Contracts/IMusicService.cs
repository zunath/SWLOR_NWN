using SWLOR.Shared.Domain.World.ValueObjects;

namespace SWLOR.Component.World.Contracts
{
    public interface IMusicService
    {
        /// <summary>
        /// When the module loads, read the ambientmusic.2da file for all active songs.
        /// Add these to the cache.
        /// </summary>
        void LoadSongList();

        /// <summary>
        /// When a player enters the server, if a battle theme has been selected by the player,
        /// apply the battle theme to the player.
        /// </summary>
        void ApplyBattleThemeToPlayer();

        /// <summary>
        /// Retrieves all of the songs loaded from the 2DA file.
        /// </summary>
        /// <returns>A list of available songs.</returns>
        List<Song> GetAllSongs();

        /// <summary>
        /// Retrieves all battle songs loaded from the 2DA file.
        /// These battle songs are able to be selected by players.
        /// </summary>
        /// <returns>A list of available battle songs players can pick from.</returns>
        Dictionary<int, Song> GetBattleSongs();
    }
}