using System;
using NWN;
using SWLOR.Game.Server.DoorRule.Contracts;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.NWScript.Enumerations;
using _ = SWLOR.Game.Server.NWScript._;

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

            float fMod = (float)(Math.Sqrt(13.0f) * Math.Sin(fOrient));
            doorPosition.X = doorPosition.X + fMod;

            fMod = (float)(Math.Sqrt(13.0f) * Math.Cos(fOrient));
            doorPosition.Y = doorPosition.Y - fMod;
            Location doorLocation = _.Location(area.Object, doorPosition, _.GetFacingFromLocation(location));

            return _.CreateObject(ObjectType.Placeable, "building_door", doorLocation);
        }
    }
}
