using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Scripting.Contracts;

namespace SWLOR.Game.Server.Scripting.Scripts.Placeable.Resource
{
    public class OnUsed: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWCreature user = (_.GetLastUsedBy());
            user.SendMessage("Use a scanner to analyze this object's resources. Use a harvester to retrieve resources from it.");
        }
    }
}
