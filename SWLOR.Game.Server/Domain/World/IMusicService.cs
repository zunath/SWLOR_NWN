using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.MusicService;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// The music of the module: the songs of the ambientmusic.2da file, the collectible songs players own as key items,
    /// each player's personal track and battle theme, and the music of an area. The static facade <c>Service.Music</c>
    /// forwards to the container's instance of this interface and keeps the event handlers.
    /// </summary>
    public interface IMusicService
    {
        /// <summary>
        /// When the module loads, read the ambientmusic.2da file for all active songs.
        /// Add these to the cache.
        /// </summary>
        void LoadSongList();

        /// <summary>
        /// When a player enters an area, grant any missing starter songs and reapply the player's
        /// personal music track and battle theme, which the area's own music otherwise replaces.
        /// Saved tracks the player has not collected are cleared.
        /// </summary>
        void ApplyPlayerMusic();

        /// <summary>
        /// Starts a player's battle theme when they attack or are attacked. The engine starts battle
        /// music only when an enemy is within the ruleset's REST_ENEMY_CHECK_DISTANCE, which SWLOR
        /// sets to 0, so it never starts on its own.
        /// </summary>
        void StartBattleThemeOnAttack();

        /// <summary>
        /// Starts a player's battle theme when they damage or are damaged by another creature.
        /// </summary>
        void StartBattleThemeOnDamage();

        /// <summary>
        /// Starts the battle theme of either combatant who is a player with one selected.
        /// </summary>
        void StartBattleTheme(uint defender, uint attacker);

        /// <summary>
        /// Stops a player's battle theme once they are no longer in combat.
        /// </summary>
        void StopBattleThemeAfterCombat();

        /// <summary>
        /// When a player leaves the module, forgets when they last fought.
        /// </summary>
        void ForgetBattleThemeCombat();

        /// <summary>
        /// Whether a player's battle theme is playing because they are in combat.
        /// </summary>
        bool IsBattleThemePlaying(uint player);

        /// <summary>
        /// Gives a player every starter song they do not already have.
        /// </summary>
        void GrantStarterSongs(uint player);

        /// <summary>
        /// Clears a player's personal track and battle theme when they are not in the player's
        /// collection, such as battle themes saved before songs had to be collected.
        /// </summary>
        /// <param name="playerId">The player's ID</param>
        /// <returns>The updated player, or null if the player is not found.</returns>
        Player ClearUncollectedMusicSettings(string playerId);

        /// <summary>
        /// Retrieves all of the songs loaded from the 2DA file.
        /// </summary>
        /// <returns>A list of available songs.</returns>
        List<Song> GetAllSongs();

        /// <summary>
        /// Retrieves every collectible song.
        /// </summary>
        IReadOnlyList<MusicSong> GetCollectibleSongs();

        /// <summary>
        /// Retrieves the collectible song tracked by a Music key item.
        /// </summary>
        bool TryGetCollectibleSong(KeyItemType keyItem, out MusicSong song);

        /// <summary>
        /// Retrieves the collectible song that plays a given ambientmusic.2da track.
        /// </summary>
        bool TryGetCollectibleSongByTrackId(int trackId, out MusicSong song);

        /// <summary>
        /// Whether an item is a song disc or a Portable Music Player.
        /// </summary>
        bool IsMusicItem(uint item);

        /// <summary>
        /// Retrieves the display name of a collectible song.
        /// </summary>
        string GetSongName(MusicSong song);

        /// <summary>
        /// Retrieves the display name of a song category.
        /// </summary>
        string GetCategoryName(MusicCategoryType category);

        /// <summary>
        /// Retrieves the track IDs of every song in a player's music collection.
        /// </summary>
        /// <param name="playerId">The player's ID</param>
        /// <returns>The collected track IDs, or an empty set if the player is not found.</returns>
        HashSet<int> GetCollectedTrackIds(string playerId);

        /// <summary>
        /// Retrieves the collection owner for a jukebox. Apartments and ships use their current owner,
        /// and jukeboxes placed directly in a city use its current mayor. City building interiors inherit
        /// mayor ownership when created, so they and unowned world jukeboxes use the selecting player's
        /// collection by returning an empty owner ID.
        /// </summary>
        /// <param name="jukebox">The jukebox placeable</param>
        /// <returns>The collection owner's player ID, or an empty string to use the selecting player's collection.</returns>
        string GetJukeboxOwnerId(uint jukebox);

        /// <summary>
        /// Plays a track for a single player only. The selection persists across areas.
        /// </summary>
        void PlayForPlayer(uint player, int trackId);

        /// <summary>
        /// Clears a player's personal track and restores the current area's music for them.
        /// If the area's music was stopped, the player's music stays off.
        /// </summary>
        void StopForPlayer(uint player);

        /// <summary>
        /// Sets or clears the battle theme a player hears in combat.
        /// </summary>
        /// <param name="player">The player</param>
        /// <param name="trackId">The track to use, or null to use each area's own battle music.</param>
        void SetBattleTheme(uint player, int? trackId);

        /// <summary>
        /// Retrieves a player's current personal track, if any.
        /// </summary>
        int? GetPersonalTrackId(uint player);

        /// <summary>
        /// Retrieves a player's battle theme, if any.
        /// </summary>
        int? GetBattleThemeId(uint player);

        /// <summary>
        /// Plays a track for everyone in an area.
        /// </summary>
        void PlayInArea(uint area, int trackId);

        /// <summary>
        /// Stops the background music for everyone in an area.
        /// </summary>
        void StopInArea(uint area);

        /// <summary>
        /// Whether an area's background music was stopped with <see cref="StopInArea"/>.
        /// The area keeps its configured tracks while stopped.
        /// </summary>
        bool IsAreaMusicStopped(uint area);

        /// <summary>
        /// Retrieves the display name of any ambientmusic.2da track.
        /// </summary>
        string GetTrackName(int trackId);
    }
}
