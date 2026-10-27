using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.MusicService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// The music of the module: the logic and state that used to be static in <c>Service.Music</c>, moved here
    /// unchanged. The facade still carries the event handlers and forwards every call to the container's instance.
    /// </summary>
    public class MusicService : IMusicService
    {
        public const string MusicPlayerTag = "MUSIC_PLAYER";
        private const string AreaMusicStoppedVariable = "MUSIC_BACKGROUND_STOPPED";
        private const string BattleThemeSetVariable = "MUSIC_BATTLE_THEME_SET";
        private const string BattleThemePlayingVariable = "MUSIC_BATTLE_THEME_PLAYING";
        // Ability-only fights can leave the engine's combat state off, so combat also lasts this long
        // after the player's last attack, damage, or hostile ability.
        private const float BattleThemeCombatGraceSeconds = 12f;
        private readonly Dictionary<uint, DateTime> _lastBattleThemeCombat = new();

        private readonly Dictionary<int, Song> _songs = new Dictionary<int,Song>();
        private readonly IReadOnlyList<MusicSong> _collectibleSongs;
        private readonly Dictionary<KeyItemType, MusicSong> _collectibleSongsByKeyItem;
        private readonly Dictionary<int, MusicSong> _collectibleSongsByTrackId;

        private readonly IDBService _db;

        public MusicService(IDBService db)
        {
            _db = db;
            _collectibleSongs = new MusicSongCatalog().Songs;
            _collectibleSongsByKeyItem = _collectibleSongs.ToDictionary(song => song.KeyItem);
            _collectibleSongsByTrackId = _collectibleSongs.ToDictionary(song => song.TrackId);
        }

        public void LoadSongList()
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

        public void ApplyPlayerMusic()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            var playerId = GetObjectUUID(player);
            if (_db.Get<Player>(playerId) == null)
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

        public void StartBattleThemeOnAttack()
        {
            var defender = OBJECT_SELF;
            StartBattleTheme(defender, GetLastAttacker(defender));
        }

        public void StartBattleThemeOnDamage()
        {
            var defender = OBJECT_SELF;
            StartBattleTheme(defender, GetLastDamager(defender));
        }

        public void StartBattleTheme(uint defender, uint attacker)
        {
            if (defender == attacker ||
                GetObjectType(defender) != ObjectType.Creature ||
                GetObjectType(attacker) != ObjectType.Creature ||
                GetArea(defender) != GetArea(attacker))
                return;

            StartBattleTheme(defender);
            StartBattleTheme(attacker);
        }

        private void StartBattleTheme(uint player)
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

        public void StopBattleThemeAfterCombat()
        {
            var player = OBJECT_SELF;
            if (GetLocalBool(player, BattleThemePlayingVariable) &&
                !GetIsInCombat(player) &&
                (!_lastBattleThemeCombat.TryGetValue(player, out var lastCombat) ||
                 (DateTime.UtcNow - lastCombat).TotalSeconds > BattleThemeCombatGraceSeconds))
                StopBattleTheme(player);
        }

        public void ForgetBattleThemeCombat()
        {
            _lastBattleThemeCombat.Remove(GetExitingObject());
        }

        public bool IsBattleThemePlaying(uint player)
        {
            return GetLocalBool(player, BattleThemePlayingVariable);
        }

        private void StopBattleTheme(uint player)
        {
            DeleteLocalBool(player, BattleThemePlayingVariable);
            _lastBattleThemeCombat.Remove(player);
            PlayerPlugin.MusicBattleToggle(player, false);

            // The client resumes background music when battle music stops. Keep stopped area music
            // off unless the player has their own track playing.
            if (IsAreaMusicStopped(GetArea(player)) && GetPersonalTrackId(player) == null)
                PlayerPlugin.MusicBackgroundToggle(player, false);
        }

        public void GrantStarterSongs(uint player)
        {
            foreach (var song in _collectibleSongs.Where(x => x.Acquisition == MusicAcquisitionType.Starter))
            {
                if (!HasKeyItem(player, song.KeyItem))
                    GiveKeyItem(player, song.KeyItem);
            }
        }

        // The key items of a player live in the stored player, which the static KeyItem service reads from the
        // database; a test subclass answers this instead.
        protected virtual bool HasKeyItem(uint player, KeyItemType keyItem) => KeyItem.HasKeyItem(player, keyItem);

        // Giving a key item saves the stored player and refreshes the key item window; a test subclass answers this
        // instead.
        protected virtual void GiveKeyItem(uint player, KeyItemType keyItem) => KeyItem.GiveKeyItem(player, keyItem);

        public Player ClearUncollectedMusicSettings(string playerId)
        {
            var dbPlayer = _db.Get<Player>(playerId);
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
            _db.Set(dbPlayer);
            return dbPlayer;
        }

        public List<Song> GetAllSongs()
        {
            return _songs.Values.ToList();
        }

        public IReadOnlyList<MusicSong> GetCollectibleSongs()
        {
            return _collectibleSongs;
        }

        public bool TryGetCollectibleSong(KeyItemType keyItem, out MusicSong song)
        {
            return _collectibleSongsByKeyItem.TryGetValue(keyItem, out song);
        }

        public bool TryGetCollectibleSongByTrackId(int trackId, out MusicSong song)
        {
            return _collectibleSongsByTrackId.TryGetValue(trackId, out song);
        }

        public bool IsMusicItem(uint item)
        {
            var tag = GetTag(item);
            if (tag == MusicPlayerTag)
                return true;

            return tag == "KEY_ITEM" &&
                   _collectibleSongsByKeyItem.ContainsKey((KeyItemType)GetLocalInt(item, "KEY_ITEM_ID"));
        }

        public string GetSongName(MusicSong song)
        {
            return KeyItem.GetKeyItem(song.KeyItem).Name;
        }

        public string GetCategoryName(MusicCategoryType category)
        {
            return category.GetAttribute<MusicCategoryType, MusicCategoryAttribute>().Name;
        }

        public HashSet<int> GetCollectedTrackIds(string playerId)
        {
            var dbPlayer = string.IsNullOrWhiteSpace(playerId) ? null : _db.Get<Player>(playerId);
            if (dbPlayer == null)
                return new HashSet<int>();

            return dbPlayer.KeyItems.Keys
                .Where(_collectibleSongsByKeyItem.ContainsKey)
                .Select(keyItem => _collectibleSongsByKeyItem[keyItem].TrackId)
                .ToHashSet();
        }

        public string GetJukeboxOwnerId(uint jukebox)
        {
            return ResolveJukeboxOwnerId(Property.GetPropertyId(jukebox), _db.Get<WorldProperty>);
        }

        private string ResolveJukeboxOwnerId(string propertyId, Func<string, WorldProperty> getProperty)
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

        public void PlayForPlayer(uint player, int trackId)
        {
            var dbPlayer = _db.Get<Player>(GetObjectUUID(player));
            if (dbPlayer == null)
                return;

            dbPlayer.Settings.PersonalMusicTrackId = trackId;
            _db.Set(dbPlayer);

            ApplyBackgroundTrackToPlayer(player, trackId, trackId);
        }

        public void StopForPlayer(uint player)
        {
            var dbPlayer = _db.Get<Player>(GetObjectUUID(player));
            if (dbPlayer == null)
                return;

            dbPlayer.Settings.PersonalMusicTrackId = null;
            _db.Set(dbPlayer);

            var area = GetArea(player);
            ApplyBackgroundTrackToPlayer(player, MusicBackgroundGetDayTrack(area), MusicBackgroundGetNightTrack(area));
            if (IsAreaMusicStopped(area))
                PlayerPlugin.MusicBackgroundToggle(player, false);
        }

        public void SetBattleTheme(uint player, int? trackId)
        {
            var dbPlayer = _db.Get<Player>(GetObjectUUID(player));
            if (dbPlayer == null)
                return;

            dbPlayer.Settings.BattleThemeId = trackId;
            _db.Set(dbPlayer);

            SetLocalBool(player, BattleThemeSetVariable, trackId != null);
            if (trackId == null && GetLocalBool(player, BattleThemePlayingVariable))
                StopBattleTheme(player);
            PlayerPlugin.MusicBattleChange(player, trackId ?? MusicBackgroundGetBattleTrack(GetArea(player)));
        }

        public int? GetPersonalTrackId(uint player)
        {
            return _db.Get<Player>(GetObjectUUID(player))?.Settings.PersonalMusicTrackId;
        }

        public int? GetBattleThemeId(uint player)
        {
            return _db.Get<Player>(GetObjectUUID(player))?.Settings.BattleThemeId;
        }

        public void PlayInArea(uint area, int trackId)
        {
            MusicBackgroundChangeDay(area, trackId);
            MusicBackgroundChangeNight(area, trackId);
            MusicBackgroundPlay(area);
            DeleteLocalBool(area, AreaMusicStoppedVariable);
        }

        public void StopInArea(uint area)
        {
            MusicBackgroundStop(area);
            SetLocalBool(area, AreaMusicStoppedVariable, true);
        }

        public bool IsAreaMusicStopped(uint area)
        {
            return GetLocalBool(area, AreaMusicStoppedVariable);
        }

        public string GetTrackName(int trackId)
        {
            if (TryGetCollectibleSongByTrackId(trackId, out var song))
                return GetSongName(song);

            return _songs.TryGetValue(trackId, out var track) ? track.DisplayName : $"Track {trackId}";
        }

        private void ApplyBackgroundTrackToPlayer(uint player, int dayTrackId, int nightTrackId)
        {
            PlayerPlugin.MusicBackgroundChangeTimeToggle(player, dayTrackId, false);
            PlayerPlugin.MusicBackgroundChangeTimeToggle(player, nightTrackId, true);
            PlayerPlugin.MusicBackgroundToggle(player, true);
        }
    }
}
