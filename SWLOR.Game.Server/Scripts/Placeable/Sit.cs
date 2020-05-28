using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN;

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
            NWObject user = _.GetLastUsedBy();
            user.AssignCommand(() =>
            {
                _.ActionSit(_.OBJECT_SELF);
            });
        }
    }
}
