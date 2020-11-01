using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature
{
    public class PersistentMapProgression
    {
        /// <summary>
        /// Saves a player's area map progression when exiting an area.
        /// </summary>
        [NWNEventHandler("area_exit")]
        [NWNEventHandler("mod_exit")]
        public static void SaveMapProgression()
        {
            var player = GetExitingObject();

            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId) ?? new Player();
            var area = OBJECT_SELF;
            var areaResref = GetResRef(area);

            if (string.IsNullOrWhiteSpace(areaResref)) return;

            var progression = Core.NWNX.Player.GetAreaExplorationState(player, area);

            dbPlayer.MapProgressions[areaResref] = progression;

            DB.Set(playerId, dbPlayer);
        }

        /// <summary>
        /// Loads a player's area map progression when entering an area for the first time after a reboot.
        /// </summary>
        [NWNEventHandler("area_enter")]
        public static void LoadMapProgression()
        {
            var player = GetEnteringObject();

            if (!GetIsPC(player) || GetIsDM(player)) return;

            var area = OBJECT_SELF;
            var areaResref = GetResRef(area);

            // Did we already load this area's progression since the last restart?
            var localVarName = $"AREA_PROGRESSION_LOADED_{areaResref}";
            if (GetLocalBool(player, localVarName)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (!dbPlayer.MapProgressions.ContainsKey(areaResref))
                return;

            var progression = dbPlayer.MapProgressions[areaResref];
            Core.NWNX.Player.SetAreaExplorationState(player, area, progression);
        }
    }
}
