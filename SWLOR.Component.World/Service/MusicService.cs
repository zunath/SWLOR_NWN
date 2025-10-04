using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.World.Contracts;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.Service;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.World.ValueObjects;

namespace SWLOR.Component.World.Service
{
    public class MusicService : IMusicService
    {
        private readonly IDatabaseService _db;
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded service to break circular dependency
        private readonly Lazy<ISongCacheService> _songCache;

        public MusicService(IDatabaseService db, IServiceProvider serviceProvider)
        {
            _db = db;
            _serviceProvider = serviceProvider;
            
            // Initialize lazy services
            _songCache = new Lazy<ISongCacheService>(() => _serviceProvider.GetRequiredService<ISongCacheService>());
        }
        
        // Lazy-loaded service to break circular dependency
        private ISongCacheService SongCache => _songCache.Value;

        /// <summary>
        /// When the module loads, read the ambientmusic.2da file for all active songs.
        /// Add these to the cache.
        /// </summary>
        public void LoadSongList()
        {
            SongCache.LoadCache();
        }

        /// <summary>
        /// When a player enters the server, if a battle theme has been selected by the player,
        /// apply the battle theme to the player.
        /// </summary>
        public void ApplyBattleThemeToPlayer()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player)) 
                return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            if (dbPlayer == null)
                return;

            if (dbPlayer.Settings.BattleThemeId == null) return;
            var area = OBJECT_SELF;
            var battleThemeId = dbPlayer.Settings.BattleThemeId ?? MusicBackgroundGetBattleTrack(area);

            PlayerPlugin.MusicBackgroundChangeTimeToggle(player, MusicBackgroundGetDayTrack(area), false);
            PlayerPlugin.MusicBackgroundChangeTimeToggle(player, MusicBackgroundGetNightTrack(area), true);
            PlayerPlugin.MusicBattleChange(player, battleThemeId);
        }

        /// <summary>
        /// Retrieves all of the songs loaded from the 2DA file.
        /// </summary>
        /// <returns>A list of available songs.</returns>
        public List<Song> GetAllSongs()
        {
            return SongCache.GetAllSongs();
        }

        /// <summary>
        /// Retrieves all battle songs loaded from the 2DA file.
        /// These battle songs are able to be selected by players.
        /// </summary>
        /// <returns>A list of available battle songs players can pick from.</returns>
        public Dictionary<int, Song> GetBattleSongs()
        {
            return SongCache.GetBattleSongs();
        }
    }
}
