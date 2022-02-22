using System.Numerics;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.DoorRule.Contracts;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Enum;

namespace SWLOR.Game.Server.DoorRule
{
    public class MediumRoundBuildingRule: IDoorRule
    {
        public NWPlaceable Run(NWArea area, Location location, float orientationOverride = 0f, float sqrtValue = 0f)
        {
            float orientationAdjustment = orientationOverride != 0f ? orientationOverride : 200.31f;
            float sqrtAdjustment = sqrtValue != 0f ? sqrtValue : 18.0f;

            Vector3 position = _.GetPositionFromLocation(location);
            float orientation = _.GetFacingFromLocation(location);

            orientation = orientation + orientationAdjustment;
            if (orientation > 360.0) orientation = orientation - 360.0f;

            float mod = _.sqrt(sqrtAdjustment) * _.sin(orientation);
            position.X = position.X + mod;

            mod = _.sqrt(sqrtAdjustment) * _.cos(orientation);
            position.Y = position.Y - mod;
            Location doorLocation = _.Location(area.Object, position, _.GetFacingFromLocation(location));

            return _.CreateObject(ObjectType.Placeable, "building_ent1", doorLocation);
        }
    }
}
