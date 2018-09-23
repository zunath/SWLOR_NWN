using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;

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
            NWObject user = (_.GetLastUsedBy());
            user.AssignCommand(() =>
            {
                _.ActionSit(Object.OBJECT_SELF);
            });

            return true;
        }
    }
}
