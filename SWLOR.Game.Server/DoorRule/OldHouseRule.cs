using NWN;
using SWLOR.Game.Server.DoorRule.Contracts;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.DoorRule
{
    public class OldHouseRule: IDoorRule
    {
        public NWPlaceable Run(NWArea area, Location location, float orientationOverride = 0f, float sqrtValue = 0f)
        {
            Vector doorPosition = _.GetPositionFromLocation(location);
            float fOrient = _.GetFacingFromLocation(location);

            fOrient = fOrient + 126.31f;
            if (fOrient > 360.0) fOrient = fOrient - 360.0f;

            float fMod = _.sqrt(13.0f) * _.sin(fOrient);
            doorPosition.m_X = doorPosition.m_X + fMod;

            fMod = _.sqrt(13.0f) * _.cos(fOrient);
            doorPosition.m_Y = doorPosition.m_Y - fMod;
            Location doorLocation = _.Location(area.Object, doorPosition, _.GetFacingFromLocation(location));

            return _.CreateObject(_.OBJECT_TYPE_PLACEABLE, "building_door", doorLocation);
        }
    }
}
