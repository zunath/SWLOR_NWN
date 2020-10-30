using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.DoorRule.Contracts
{
    public interface IDoorRule
    {
        NWPlaceable Run(uint area, Location location, float orientationOverride = 0f, float sqrtValue = 0);
    }
}
