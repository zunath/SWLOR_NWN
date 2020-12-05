using System.Numerics;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.DoorRule.Contracts;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Enum;

namespace SWLOR.Game.Server.DoorRule
{
    public class OldHouseRule: IDoorRule
    {
        public NWPlaceable Run(NWArea area, Location location, float orientationOverride = 0f, float sqrtValue = 0f)
        {
            Vector3 doorPosition = _.GetPositionFromLocation(location);
            float fOrient = _.GetFacingFromLocation(location);

            fOrient = fOrient + 126.31f;
            if (fOrient > 360.0) fOrient = fOrient - 360.0f;

            float fMod = _.sqrt(13.0f) * _.sin(fOrient);
            doorPosition.X = doorPosition.X + fMod;

            fMod = _.sqrt(13.0f) * _.cos(fOrient);
            doorPosition.Y = doorPosition.Y - fMod;
            Location doorLocation = _.Location(area.Object, doorPosition, _.GetFacingFromLocation(location));

            return _.CreateObject(ObjectType.Placeable, "building_door", doorLocation);
        }
    }
}
