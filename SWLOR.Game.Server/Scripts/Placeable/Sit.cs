using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Scripts.Placeable
{
    public class Sit: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWObject user = NWScript.GetLastUsedBy();
            user.AssignCommand(() =>
            {
                NWScript.ActionSit(NWScript.OBJECT_SELF);
            });
        }
    }
}
