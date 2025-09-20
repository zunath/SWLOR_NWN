
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.NWN.API.NWNX;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Event;
using SWLOR.Shared.Core.Log;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Core.Service;

namespace SWLOR.Game.Server.Feature
{
    public class PersistentMapProgression
    {
        private static ILogger _logger = ServiceContainer.GetService<ILogger>();
        private static readonly IDatabaseService _db = ServiceContainer.GetService<IDatabaseService>();
        /// <summary>
        /// Saves a player's area map progression when exiting an area.
        /// </summary>
        [ScriptHandler(ScriptName.OnAreaExit)]
        [ScriptHandler(ScriptName.OnModuleExit)]
        public static void SaveMapProgression()
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
        [ScriptHandler(ScriptName.OnAreaEnter)]
        public static void LoadMapProgression()
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
                    if (KeyItem.HasKeyItem(player, keyItemType))
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
