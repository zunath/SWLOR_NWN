using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.MusicService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    public class Music
    {
        public const string MusicPlayerTag = "MUSIC_PLAYER";
        private const string AreaMusicStoppedVariable = "MUSIC_BACKGROUND_STOPPED";
        private const string BattleThemeSetVariable = "MUSIC_BATTLE_THEME_SET";
        private const string BattleThemePlayingVariable = "MUSIC_BATTLE_THEME_PLAYING";
        // Ability-only fights can leave the engine's combat state off, so combat also lasts this long
        // after the player's last attack, damage, or hostile ability.
        private const float BattleThemeCombatGraceSeconds = 12f;
        private static readonly Dictionary<uint, DateTime> _lastBattleThemeCombat = new();

        private static readonly Dictionary<int, Song> _songs = new Dictionary<int,Song>();
        private static readonly Dictionary<KeyItemType, MusicSong> _collectibleSongsByKeyItem = MusicSongCatalog.Songs
            .ToDictionary(song => song.KeyItem);
        private static readonly Dictionary<int, MusicSong> _collectibleSongsByTrackId = MusicSongCatalog.Songs
            .ToDictionary(song => song.TrackId);

        public class Song
        {
            public int ID { get; }
            public string DisplayName { get; }

            public Song(int id, string displayName)
            {
                ID = id;
                DisplayName = displayName;
            }
        }

        /// <summary>
        /// When the module loads, read the ambientmusic.2da file for all active songs.
        /// Add these to the cache.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void LoadSongList()
        {
            const string File = "ambientmusic";
            var rowCount = Get2DARowCount(File);

            for (var row = 0; row < rowCount; row++)
            {
                var description = Get2DAString(File, "Description", row);
                var displayName = Get2DAString(File, "DisplayName", row);

                // Skip record if a name cannot be determined.
                if (string.IsNullOrWhiteSpace(description) &&
                    string.IsNullOrWhiteSpace(displayName)) continue;

                string name = string.IsNullOrWhiteSpace(description) ?
                    displayName :
                    GetStringByStrRef(Convert.ToInt32(description));

                _songs[row] = new Song(row, name);
            }
        }

        /// <summary>
        /// When a player enters an area, grant any missing starter songs and reapply the player's
        /// personal music track and battle theme, which the area's own music otherwise replaces.
        /// Saved tracks the player has not collected are cleared.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void ApplyPlayerMusic()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            var playerId = GetObjectUUID(player);
            if (DB.Get<Player>(playerId) == null)
                return;

            GrantStarterSongs(player);
            var dbPlayer = ClearUncollectedMusicSettings(playerId);

            var area = OBJECT_SELF;
            var personalTrackId = dbPlayer.Settings.PersonalMusicTrackId;
            if (personalTrackId != null)
            {
                ApplyBackgroundTrackToPlayer(player, personalTrackId.Value, personalTrackId.Value);
            }

            if (GetLocalBool(player, BattleThemePlayingVariable))
                StopBattleTheme(player);
            SetLocalBool(player, BattleThemeSetVariable, dbPlayer.Settings.BattleThemeId != null);
            if (dbPlayer.Settings.BattleThemeId != null)
            {
                PlayerPlugin.MusicBattleChange(player, dbPlayer.Settings.BattleThemeId.Value);
            }
            else if (personalTrackId != null)
            {
                PlayerPlugin.MusicBattleChange(player, MusicBackgroundGetBattleTrack(area));
            }
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
            var defender = OBJECT_SELF;
            StartBattleTheme(defender, GetLastAttacker(defender));
        }

        /// <summary>
        /// Starts a player's battle theme when they damage or are damaged by another creature.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureDamagedBefore)]
        [NWNEventHandler(ScriptName.OnPlayerDamaged)]
        public static void StartBattleThemeOnDamage()
        {
            var defender = OBJECT_SELF;
            StartBattleTheme(defender, GetLastDamager(defender));
        }

        /// <summary>
        /// Starts the battle theme of either combatant who is a player with one selected.
        /// </summary>
        public static void StartBattleTheme(uint defender, uint attacker)
        {
            if (defender == attacker ||
                GetObjectType(defender) != ObjectType.Creature ||
                GetObjectType(attacker) != ObjectType.Creature ||
                GetArea(defender) != GetArea(attacker))
                return;

            StartBattleTheme(defender);
            StartBattleTheme(attacker);
        }

        private static void StartBattleTheme(uint player)
        {
            if (!GetLocalBool(player, BattleThemeSetVariable) ||
                !GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            _lastBattleThemeCombat[player] = DateTime.UtcNow;
            if (GetLocalBool(player, BattleThemePlayingVariable))
                return;

            SetLocalBool(player, BattleThemePlayingVariable, true);
            PlayerPlugin.MusicBattleToggle(player, true);
        }

        /// <summary>
        /// Stops a player's battle theme once they are no longer in combat.
        /// </summary>
        [NWNEventHandler(ScriptName.OnIntervalPC6Seconds)]
        public static void StopBattleThemeAfterCombat()
        {
            var player = OBJECT_SELF;
            if (GetLocalBool(player, BattleThemePlayingVariable) &&
                !GetIsInCombat(player) &&
                (!_lastBattleThemeCombat.TryGetValue(player, out var lastCombat) ||
                 (DateTime.UtcNow - lastCombat).TotalSeconds > BattleThemeCombatGraceSeconds))
                StopBattleTheme(player);
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void ForgetBattleThemeCombat()
        {
            _lastBattleThemeCombat.Remove(GetExitingObject());
        }

        /// <summary>
        /// Whether a player's battle theme is playing because they are in combat.
        /// </summary>
        public static bool IsBattleThemePlaying(uint player)
        {
            return GetLocalBool(player, BattleThemePlayingVariable);
        }

        private static void StopBattleTheme(uint player)
        {
            DeleteLocalBool(player, BattleThemePlayingVariable);
            _lastBattleThemeCombat.Remove(player);
            PlayerPlugin.MusicBattleToggle(player, false);

            // The client resumes background music when battle music stops. Keep stopped area music
            // off unless the player has their own track playing.
            if (IsAreaMusicStopped(GetArea(player)) && GetPersonalTrackId(player) == null)
                PlayerPlugin.MusicBackgroundToggle(player, false);
        }

        /// <summary>
        /// Gives a player every starter song they do not already have.
        /// </summary>
        public static void GrantStarterSongs(uint player)
        {
            foreach (var song in MusicSongCatalog.Songs.Where(x => x.Acquisition == MusicAcquisitionType.Starter))
            {
                if (!KeyItem.HasKeyItem(player, song.KeyItem))
                    KeyItem.GiveKeyItem(player, song.KeyItem);
            }
        }

        /// <summary>
        /// Clears a player's personal track and battle theme when they are not in the player's
        /// collection, such as battle themes saved before songs had to be collected.
        /// </summary>
        /// <param name="playerId">The player's ID</param>
        /// <returns>The updated player, or null if the player is not found.</returns>
        public static Player ClearUncollectedMusicSettings(string playerId)
        {
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null)
                return null;

            var collected = GetCollectedTrackIds(playerId);
            var isPersonalTrackCollected = dbPlayer.Settings.PersonalMusicTrackId == null ||
                                           collected.Contains(dbPlayer.Settings.PersonalMusicTrackId.Value);
            var isBattleThemeCollected = dbPlayer.Settings.BattleThemeId == null ||
                                         collected.Contains(dbPlayer.Settings.BattleThemeId.Value);
            if (isPersonalTrackCollected && isBattleThemeCollected)
                return dbPlayer;

            if (!isPersonalTrackCollected)
                dbPlayer.Settings.PersonalMusicTrackId = null;
            if (!isBattleThemeCollected)
                dbPlayer.Settings.BattleThemeId = null;
            DB.Set(dbPlayer);
            return dbPlayer;
        }

        /// <summary>
        /// Retrieves all of the songs loaded from the 2DA file.
        /// </summary>
        /// <returns>A list of available songs.</returns>
        public static List<Song> GetAllSongs()
        {
            return _songs.Values.ToList();
        }

        /// <summary>
        /// Retrieves every collectible song.
        /// </summary>
        public static IReadOnlyList<MusicSong> GetCollectibleSongs()
        {
            return MusicSongCatalog.Songs;
        }

        /// <summary>
        /// Retrieves the collectible song tracked by a Music key item.
        /// </summary>
        public static bool TryGetCollectibleSong(KeyItemType keyItem, out MusicSong song)
        {
            return _collectibleSongsByKeyItem.TryGetValue(keyItem, out song);
        }

        /// <summary>
        /// Retrieves the collectible song that plays a given ambientmusic.2da track.
        /// </summary>
        public static bool TryGetCollectibleSongByTrackId(int trackId, out MusicSong song)
        {
            return _collectibleSongsByTrackId.TryGetValue(trackId, out song);
        }

        /// <summary>
        /// Whether an item is a song disc or a Portable Music Player.
        /// </summary>
        public static bool IsMusicItem(uint item)
        {
            var tag = GetTag(item);
            if (tag == MusicPlayerTag)
                return true;

            return tag == "KEY_ITEM" &&
                   _collectibleSongsByKeyItem.ContainsKey((KeyItemType)GetLocalInt(item, "KEY_ITEM_ID"));
        }

        /// <summary>
        /// Retrieves the display name of a collectible song.
        /// </summary>
        public static string GetSongName(MusicSong song)
        {
            return KeyItem.GetKeyItem(song.KeyItem).Name;
        }

        /// <summary>
        /// Retrieves the display name of a song category.
        /// </summary>
        public static string GetCategoryName(MusicCategoryType category)
        {
            return category.GetAttribute<MusicCategoryType, MusicCategoryAttribute>().Name;
        }

        /// <summary>
        /// Retrieves the track IDs of every song in a player's music collection.
        /// </summary>
        /// <param name="playerId">The player's ID</param>
        /// <returns>The collected track IDs, or an empty set if the player is not found.</returns>
        public static HashSet<int> GetCollectedTrackIds(string playerId)
        {
            var dbPlayer = string.IsNullOrWhiteSpace(playerId) ? null : DB.Get<Player>(playerId);
            if (dbPlayer == null)
                return new HashSet<int>();

            return dbPlayer.KeyItems.Keys
                .Where(_collectibleSongsByKeyItem.ContainsKey)
                .Select(keyItem => _collectibleSongsByKeyItem[keyItem].TrackId)
                .ToHashSet();
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
            return ResolveJukeboxOwnerId(Property.GetPropertyId(jukebox), DB.Get<WorldProperty>);
        }

        private static string ResolveJukeboxOwnerId(string propertyId, Func<string, WorldProperty> getProperty)
        {
            if (string.IsNullOrWhiteSpace(propertyId))
                return string.Empty;

            var property = getProperty(propertyId);
            WorldProperty containingProperty = null;
            var visited = new HashSet<string>();
            while (property != null && visited.Add(property.Id))
            {
                if (containingProperty == null && property.PropertyType != PropertyType.Structure)
                    containingProperty = property;

                if (property.PropertyType == PropertyType.City)
                    return containingProperty == property ? property.OwnerPlayerId ?? string.Empty : string.Empty;

                if (string.IsNullOrWhiteSpace(property.ParentPropertyId))
                    break;

                var parent = getProperty(property.ParentPropertyId);
                if (parent == null)
                    break;

                property = parent;
            }

            return (containingProperty ?? property)?.OwnerPlayerId ?? string.Empty;
        }

        /// <summary>
        /// Plays a track for a single player only. The selection persists across areas.
        /// </summary>
        public static void PlayForPlayer(uint player, int trackId)
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            if (dbPlayer == null)
                return;

            dbPlayer.Settings.PersonalMusicTrackId = trackId;
            DB.Set(dbPlayer);

            ApplyBackgroundTrackToPlayer(player, trackId, trackId);
        }

        /// <summary>
        /// Clears a player's personal track and restores the current area's music for them.
        /// If the area's music was stopped, the player's music stays off.
        /// </summary>
        public static void StopForPlayer(uint player)
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            if (dbPlayer == null)
                return;

            dbPlayer.Settings.PersonalMusicTrackId = null;
            DB.Set(dbPlayer);

            var area = GetArea(player);
            ApplyBackgroundTrackToPlayer(player, MusicBackgroundGetDayTrack(area), MusicBackgroundGetNightTrack(area));
            if (IsAreaMusicStopped(area))
                PlayerPlugin.MusicBackgroundToggle(player, false);
        }

        /// <summary>
        /// Sets or clears the battle theme a player hears in combat.
        /// </summary>
        /// <param name="player">The player</param>
        /// <param name="trackId">The track to use, or null to use each area's own battle music.</param>
        public static void SetBattleTheme(uint player, int? trackId)
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            if (dbPlayer == null)
                return;

            dbPlayer.Settings.BattleThemeId = trackId;
            DB.Set(dbPlayer);

            SetLocalBool(player, BattleThemeSetVariable, trackId != null);
            if (trackId == null && GetLocalBool(player, BattleThemePlayingVariable))
                StopBattleTheme(player);
            PlayerPlugin.MusicBattleChange(player, trackId ?? MusicBackgroundGetBattleTrack(GetArea(player)));
        }

        /// <summary>
        /// Retrieves a player's current personal track, if any.
        /// </summary>
        public static int? GetPersonalTrackId(uint player)
        {
            return DB.Get<Player>(GetObjectUUID(player))?.Settings.PersonalMusicTrackId;
        }

        /// <summary>
        /// Retrieves a player's battle theme, if any.
        /// </summary>
        public static int? GetBattleThemeId(uint player)
        {
            return DB.Get<Player>(GetObjectUUID(player))?.Settings.BattleThemeId;
        }

        /// <summary>
        /// Plays a track for everyone in an area.
        /// </summary>
        public static void PlayInArea(uint area, int trackId)
        {
            MusicBackgroundChangeDay(area, trackId);
            MusicBackgroundChangeNight(area, trackId);
            MusicBackgroundPlay(area);
            DeleteLocalBool(area, AreaMusicStoppedVariable);
        }

        /// <summary>
        /// Stops the background music for everyone in an area.
        /// </summary>
        public static void StopInArea(uint area)
        {
            MusicBackgroundStop(area);
            SetLocalBool(area, AreaMusicStoppedVariable, true);
        }

        /// <summary>
        /// Whether an area's background music was stopped with <see cref="StopInArea"/>.
        /// The area keeps its configured tracks while stopped.
        /// </summary>
        public static bool IsAreaMusicStopped(uint area)
        {
            return GetLocalBool(area, AreaMusicStoppedVariable);
        }

        /// <summary>
        /// Retrieves the display name of any ambientmusic.2da track.
        /// </summary>
        public static string GetTrackName(int trackId)
        {
            if (TryGetCollectibleSongByTrackId(trackId, out var song))
                return GetSongName(song);

            return _songs.TryGetValue(trackId, out var track) ? track.DisplayName : $"Track {trackId}";
        }

        private static void ApplyBackgroundTrackToPlayer(uint player, int dayTrackId, int nightTrackId)
        {
            PlayerPlugin.MusicBackgroundChangeTimeToggle(player, dayTrackId, false);
            PlayerPlugin.MusicBackgroundChangeTimeToggle(player, nightTrackId, true);
            PlayerPlugin.MusicBackgroundToggle(player, true);
        }
    }
}
