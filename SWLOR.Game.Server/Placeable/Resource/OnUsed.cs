using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;

namespace SWLOR.Game.Server.Placeable.Resource
{
    public class OnUsed: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWCreature user = (_.GetLastUsedBy());
            user.SendMessage("Use a scanner to analyze this object's resources. Use a harvester to retrieve resources from it.");
            return true;
        }
    }
}
