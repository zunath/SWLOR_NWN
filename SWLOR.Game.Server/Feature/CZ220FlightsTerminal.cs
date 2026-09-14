using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature
{
    /// <summary>
    /// Spawns the CZ-220 starport flights terminal in the station hangar at module load.
    /// CZ-220 has no builder-placed terminal, so players there previously relied on a direct
    /// point-to-point transport NPC. Placing a terminal lets them use the standard shuttle system.
    /// </summary>
    public class CZ220FlightsTerminal
    {
        private const string HangarAreaResref = "czs220_hangar";

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void SpawnHangarFlightsTerminal()
        {
            var area = Area.GetAreaByResref(HangarAreaResref);
            if (!GetIsObjectValid(area))
            {
                Log.WriteStructured(
                    LogGroup.Server,
                    "Unable to spawn CZ-220 flights terminal: hangar area not found. AreaResref={AreaResref}",
                    HangarAreaResref);
                return;
            }

            // Open floor in the hangar between the transport attendant and the landing point.
            // Orientation (in degrees) turns the console's face toward the walkway rather than the wall.
            var location = Location(area, new Vector3(33.0f, 34.4f, 0f), 270.0f);
            var terminal = CreateObject(ObjectType.Placeable, "flights_terminal", location);

            SetName(terminal, "CZ-220 Starport Flights Terminal");
            SetPlotFlag(terminal, true);
            SetLocalInt(terminal, "CURRENT_LOCATION", (int)PlanetType.CZ220);
            SetEventScript(terminal, EventScript.Placeable_OnUsed, ScriptName.OnPlaceableGenericConversation);
        }
    }
}
