using Microsoft.Extensions.DependencyInjection;
using SWLOR.NWN.API.NWNX;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Domain.Common.Enums;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Inventory.Enums;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Area;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Component.Character.Feature
{
    public class PersistentMapProgression
    {
        private readonly ILogger _logger;
        private readonly IDatabaseService _db;
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded services to break circular dependencies
        private IKeyItemService KeyItemService => _serviceProvider.GetRequiredService<IKeyItemService>();

        public PersistentMapProgression(ILogger logger, IDatabaseService db, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _db = db;
            // Services are now lazy-loaded via IServiceProvider
        }
        /// <summary>
        /// Saves a player's area map progression when exiting an area.
        /// </summary>
        [ScriptHandler<OnAreaExit>]
        [ScriptHandler<OnModuleExit>]
        public void SaveMapProgression()
        {
            var player = GetExitingObject();

            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId) ?? new Player(playerId);
            var area = OBJECT_SELF;
            var areaResref = GetResRef(area);

            if (string.IsNullOrWhiteSpace(areaResref)) return;

            var progression = PlayerPlugin.GetAreaExplorationState(player, area);

            dbPlayer.MapProgressions[areaResref] = progression;

            _db.Set(dbPlayer);
        }

        /// <summary>
        /// Loads a player's area map progression when entering an area for the first time after a reboot.
        /// </summary>
        [ScriptHandler<OnAreaEnter>]
        public void LoadMapProgression()
        {
            var player = GetEnteringObject();

            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player)) 
                return;

            var area = OBJECT_SELF;
            var mapKeyItemId = GetLocalInt(area, "MAP_KEY_ITEM_ID");

            // If the area has a map associated and the player has this key item,
            // exit early. There's no reason to load their progression - the map explores it for them automatically.
            if (mapKeyItemId > 0)
            {
                try
                {
                    var keyItemType = (KeyItemType)mapKeyItemId;
                    if (KeyItemService.HasKeyItem(player, keyItemType))
                        return;
                }
                catch
                {
                    _logger.Write<ErrorLogGroup>($"MAP_KEY_ITEM_ID '{mapKeyItemId}' is misconfigured on area '{GetName(area)}'.");
                }
            }
            
            var areaResref = GetResRef(area);

            // Did we already load this area's progression since the last restart?
            var localVarName = $"AREA_PROGRESSION_LOADED_{areaResref}";
            if (GetLocalBool(player, localVarName)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            if (dbPlayer == null)
                return;

            if (!dbPlayer.MapProgressions.ContainsKey(areaResref))
                return;

            var progression = dbPlayer.MapProgressions[areaResref];
            PlayerPlugin.SetAreaExplorationState(player, area, progression);
        }
    }
}
