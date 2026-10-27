using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.MusicService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the module's music. The logic and state live in <see cref="IMusicService"/>; every member here
    /// forwards to the container's instance, and the event handlers stay here so their names in the handler order
    /// manifest do not change.
    /// </summary>
    public static class Music
    {
        public const string MusicPlayerTag = Domain.World.MusicService.MusicPlayerTag;

        private static IMusicService Instance => FacadeAccessor.Resolve<IMusicService>();

        /// <summary>
        /// When the module loads, read the ambientmusic.2da file for all active songs.
        /// Add these to the cache.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void LoadSongList()
        {
            Instance.LoadSongList();
        }

        /// <summary>
        /// When a player enters an area, grant any missing starter songs and reapply the player's
        /// personal music track and battle theme, which the area's own music otherwise replaces.
        /// Saved tracks the player has not collected are cleared.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void ApplyPlayerMusic()
        {
            Instance.ApplyPlayerMusic();
        }

        /// <summary>
        /// Starts a player's battle theme when they attack or are attacked. The engine starts battle
        /// music only when an enemy is within the ruleset's REST_ENEMY_CHECK_DISTANCE, which SWLOR
        /// sets to 0, so it never starts on its own.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureAttackBefore)]
        [NWNEventHandler(ScriptName.OnPlayerAttacked)]
        public static void StartBattleThemeOnAttack()
        {
            Instance.StartBattleThemeOnAttack();
        }

        /// <summary>
        /// Starts a player's battle theme when they damage or are damaged by another creature.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureDamagedBefore)]
        [NWNEventHandler(ScriptName.OnPlayerDamaged)]
        public static void StartBattleThemeOnDamage()
        {
            Instance.StartBattleThemeOnDamage();
        }

        /// <summary>
        /// Starts the battle theme of either combatant who is a player with one selected.
        /// </summary>
        public static void StartBattleTheme(uint defender, uint attacker)
        {
            Instance.StartBattleTheme(defender, attacker);
        }

        /// <summary>
        /// Stops a player's battle theme once they are no longer in combat.
        /// </summary>
        [NWNEventHandler(ScriptName.OnIntervalPC6Seconds)]
        public static void StopBattleThemeAfterCombat()
        {
            Instance.StopBattleThemeAfterCombat();
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void ForgetBattleThemeCombat()
        {
            Instance.ForgetBattleThemeCombat();
        }

        /// <summary>
        /// Whether a player's battle theme is playing because they are in combat.
        /// </summary>
        public static bool IsBattleThemePlaying(uint player)
        {
            return Instance.IsBattleThemePlaying(player);
        }

        /// <summary>
        /// Gives a player every starter song they do not already have.
        /// </summary>
        public static void GrantStarterSongs(uint player)
        {
            Instance.GrantStarterSongs(player);
        }

        /// <summary>
        /// Clears a player's personal track and battle theme when they are not in the player's
        /// collection, such as battle themes saved before songs had to be collected.
        /// </summary>
        /// <param name="playerId">The player's ID</param>
        /// <returns>The updated player, or null if the player is not found.</returns>
        public static Player ClearUncollectedMusicSettings(string playerId)
        {
            return Instance.ClearUncollectedMusicSettings(playerId);
        }

        /// <summary>
        /// Retrieves all of the songs loaded from the 2DA file.
        /// </summary>
        /// <returns>A list of available songs.</returns>
        public static List<Song> GetAllSongs()
        {
            return Instance.GetAllSongs();
        }

        /// <summary>
        /// Retrieves every collectible song.
        /// </summary>
        public static IReadOnlyList<MusicSong> GetCollectibleSongs()
        {
            return Instance.GetCollectibleSongs();
        }

        /// <summary>
        /// Retrieves the collectible song tracked by a Music key item.
        /// </summary>
        public static bool TryGetCollectibleSong(KeyItemType keyItem, out MusicSong song)
        {
            return Instance.TryGetCollectibleSong(keyItem, out song);
        }

        /// <summary>
        /// Retrieves the collectible song that plays a given ambientmusic.2da track.
        /// </summary>
        public static bool TryGetCollectibleSongByTrackId(int trackId, out MusicSong song)
        {
            return Instance.TryGetCollectibleSongByTrackId(trackId, out song);
        }

        /// <summary>
        /// Whether an item is a song disc or a Portable Music Player.
        /// </summary>
        public static bool IsMusicItem(uint item)
        {
            return Instance.IsMusicItem(item);
        }

        /// <summary>
        /// Retrieves the display name of a collectible song.
        /// </summary>
        public static string GetSongName(MusicSong song)
        {
            return Instance.GetSongName(song);
        }

        /// <summary>
        /// Retrieves the display name of a song category.
        /// </summary>
        public static string GetCategoryName(MusicCategoryType category)
        {
            return Instance.GetCategoryName(category);
        }

        /// <summary>
        /// Retrieves the track IDs of every song in a player's music collection.
        /// </summary>
        /// <param name="playerId">The player's ID</param>
        /// <returns>The collected track IDs, or an empty set if the player is not found.</returns>
        public static HashSet<int> GetCollectedTrackIds(string playerId)
        {
            return Instance.GetCollectedTrackIds(playerId);
        }

        /// <summary>
        /// Retrieves the collection owner for a jukebox. Apartments and ships use their current owner,
        /// and jukeboxes placed directly in a city use its current mayor. City building interiors inherit
        /// mayor ownership when created, so they and unowned world jukeboxes use the selecting player's
        /// collection by returning an empty owner ID.
        /// </summary>
        /// <param name="jukebox">The jukebox placeable</param>
        /// <returns>The collection owner's player ID, or an empty string to use the selecting player's collection.</returns>
        public static string GetJukeboxOwnerId(uint jukebox)
        {
            return Instance.GetJukeboxOwnerId(jukebox);
        }

        /// <summary>
        /// Plays a track for a single player only. The selection persists across areas.
        /// </summary>
        public static void PlayForPlayer(uint player, int trackId)
        {
            Instance.PlayForPlayer(player, trackId);
        }

        /// <summary>
        /// Clears a player's personal track and restores the current area's music for them.
        /// If the area's music was stopped, the player's music stays off.
        /// </summary>
        public static void StopForPlayer(uint player)
        {
            Instance.StopForPlayer(player);
        }

        /// <summary>
        /// Sets or clears the battle theme a player hears in combat.
        /// </summary>
        /// <param name="player">The player</param>
        /// <param name="trackId">The track to use, or null to use each area's own battle music.</param>
        public static void SetBattleTheme(uint player, int? trackId)
        {
            Instance.SetBattleTheme(player, trackId);
        }

        /// <summary>
        /// Retrieves a player's current personal track, if any.
        /// </summary>
        public static int? GetPersonalTrackId(uint player)
        {
            return Instance.GetPersonalTrackId(player);
        }

        /// <summary>
        /// Retrieves a player's battle theme, if any.
        /// </summary>
        public static int? GetBattleThemeId(uint player)
        {
            return Instance.GetBattleThemeId(player);
        }

        /// <summary>
        /// Plays a track for everyone in an area.
        /// </summary>
        public static void PlayInArea(uint area, int trackId)
        {
            Instance.PlayInArea(area, trackId);
        }

        /// <summary>
        /// Stops the background music for everyone in an area.
        /// </summary>
        public static void StopInArea(uint area)
        {
            Instance.StopInArea(area);
        }

        /// <summary>
        /// Whether an area's background music was stopped with <see cref="StopInArea"/>.
        /// The area keeps its configured tracks while stopped.
        /// </summary>
        public static bool IsAreaMusicStopped(uint area)
        {
            return Instance.IsAreaMusicStopped(area);
        }

        /// <summary>
        /// Retrieves the display name of any ambientmusic.2da track.
        /// </summary>
        public static string GetTrackName(int trackId)
        {
            return Instance.GetTrackName(trackId);
        }
    }
}
