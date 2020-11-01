using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Legacy.DoorRule.Contracts;
using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.DoorRule
{
    public class LargeSquareBuildingRule: IDoorRule
    {
        public NWPlaceable Run(uint area, Location location, float orientationOverride = -1f, float sqrtValue = -1f)
        {
            var orientationAdjustment = orientationOverride != 0f ? orientationOverride : 90f;
            var sqrtAdjustment = sqrtValue != 0f ? sqrtValue : 34f;

            var position = NWScript.GetPositionFromLocation(location);
            var orientation = NWScript.GetFacingFromLocation(location);

            orientation = orientation + orientationAdjustment;
            if (orientation > 360.0) orientation = orientation - 360.0f;

            var mod = NWScript.sqrt(sqrtAdjustment) * NWScript.sin(orientation);
            position.X = position.X + mod;

            mod = NWScript.sqrt(sqrtAdjustment) * NWScript.cos(orientation);
            position.Y = position.Y - mod;
            var doorLocation = NWScript.Location(area, position, NWScript.GetFacingFromLocation(location));

            return NWScript.CreateObject(ObjectType.Placeable, "building_ent1", doorLocation);
        }
    }
}
