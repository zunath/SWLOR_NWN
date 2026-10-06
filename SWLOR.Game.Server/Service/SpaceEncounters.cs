using System;
using System.Linq;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    public static partial class Space
    {
        private static void ProcessEncounterAI(uint creature, uint target, ShipStatus status, DateTime now)
        {
            var profile = SpaceEncounterCatalog.Default.Profiles[status.EncounterProfile];
            if (!IsOperatingShip(target) || GetShipStatus(target)?.Hull <= 0 || GetArea(creature) != GetArea(target) ||
                !(GetIsEnemy(creature, target) || GetIsEnemy(target, creature))) return;
            var distance = GetDistanceBetween(creature, target);
            AssignCommand(creature, () =>
            {
                ClearAllActions();
                if (distance > profile.Range * .85) ActionMoveToObject(target, true, (float)(profile.Range * .75));
                else if (distance < profile.Range * .4) ActionMoveAwayFromObject(target, true, (float)(profile.Range * .55));
            });
            if (status.GlobalRecast > now) return;
            SetCurrentTarget(creature, target);
            var modules = status.HighPowerModules.OrderBy(x => x.Key).ToArray();
            for (var index = 0; index < modules.Length; index++)
            {
                var next = (status.NextEncounterWeapon + index) % modules.Length;
                var fitted = modules[next].Value;
                if (fitted.RecastTime > now) continue;
                if (ActivateFittedModule(creature, fitted.ItemInstanceId))
                { status.NextEncounterWeapon = (next + 1) % modules.Length; break; }
            }
        }
    }
}
