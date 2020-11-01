using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.Scripts.Placeable.DisabledStructure
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
            NWPlayer user = (NWScript.GetLastUsedBy());

            user.SendMessage("The base is currently out of fuel and this object cannot be powered online.");

        }
    }
}
