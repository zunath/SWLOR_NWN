using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.DoorRule.Contracts
{
    public interface IDoorRule
    {
        NWPlaceable Run(uint area, Location location, float orientationOverride = 0f, float sqrtValue = 0);
    }
}
