using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;

namespace SWLOR.Game.Server.Placeable.Resource
{
    public class OnUsed: IRegisteredEvent
    {
        private readonly INWScript _;

        public OnUsed(INWScript script)
        {
            _ = script;
        }

        public bool Run(params object[] args)
        {
            Object self = Object.OBJECT_SELF;
            NWObject oPC = NWObject.Wrap(_.GetLastUsedBy());
            oPC.AssignCommand(() => _.ActionAttack(self));
            return true;
        }
    }
}
