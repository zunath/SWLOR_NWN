using NWN;
using SWLOR.Game.Server.DoorRule.Contracts;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.DoorRule
{
    public class MediumRoundBuildingRule: IDoorRule
    {
        public NWPlaceable Run(NWArea area, Location location, float orientationOverride = 0f, float sqrtValue = 0f)
        {
            float orientationAdjustment = orientationOverride != 0f ? orientationOverride : 200.31f;
            float sqrtAdjustment = sqrtValue != 0f ? sqrtValue : 18.0f;

            Vector position = _.GetPositionFromLocation(location);
            float orientation = _.GetFacingFromLocation(location);

            orientation = orientation + orientationAdjustment;
            if (orientation > 360.0) orientation = orientation - 360.0f;

            float mod = _.sqrt(sqrtAdjustment) * _.sin(orientation);
            position.m_X = position.m_X + mod;

            mod = _.sqrt(sqrtAdjustment) * _.cos(orientation);
            position.m_Y = position.m_Y - mod;
            Location doorLocation = _.Location(area.Object, position, _.GetFacingFromLocation(location));

            return _.CreateObject(_.OBJECT_TYPE_PLACEABLE, "building_ent1", doorLocation);
        }
    }
}
