using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Legacy.DoorRule.Contracts;
using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.DoorRule
{
    public class OldHouseRule: IDoorRule
    {
        public NWPlaceable Run(uint area, Location location, float orientationOverride = 0f, float sqrtValue = 0f)
        {
            var doorPosition = NWScript.GetPositionFromLocation(location);
            var fOrient = NWScript.GetFacingFromLocation(location);

            fOrient = fOrient + 126.31f;
            if (fOrient > 360.0) fOrient = fOrient - 360.0f;

            var fMod = NWScript.sqrt(13.0f) * NWScript.sin(fOrient);
            doorPosition.X = doorPosition.X + fMod;

            fMod = NWScript.sqrt(13.0f) * NWScript.cos(fOrient);
            doorPosition.Y = doorPosition.Y - fMod;
            var doorLocation = NWScript.Location(area, doorPosition, NWScript.GetFacingFromLocation(location));

            return NWScript.CreateObject(ObjectType.Placeable, "building_door", doorLocation);
        }
    }
}
