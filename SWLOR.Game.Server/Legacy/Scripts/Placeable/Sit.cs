using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.Scripts.Placeable
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
