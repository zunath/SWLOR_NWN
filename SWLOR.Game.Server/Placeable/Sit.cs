using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Contracts;
using SWLOR.Game.Server.NWN.NWScript;

namespace SWLOR.Game.Server.Placeable
{
    public class Sit: IRegisteredEvent
    {
        private readonly INWScript _;

        public Sit(INWScript script)
        {
            _ = script;
        }

        public bool Run(params object[] args)
        {
            NWObject user = NWObject.Wrap(_.GetLastUsedBy());
            user.AssignCommand(() =>
            {
                _.ActionSit(Object.OBJECT_SELF);
            });

            return true;
        }
    }
}
