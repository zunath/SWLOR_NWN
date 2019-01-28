using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Player
{
    public class OnPlayerHeartbeat: IRegisteredEvent
    {
        private readonly ISpaceService _space;

        public OnPlayerHeartbeat(ISpaceService space)
        {
            _space = space;
        }

        public bool Run(params object[] args)
        {
            NWCreature self = Object.OBJECT_SELF;
            _space.OnCreatureHeartbeat(self);

            return true;
        }
    }
}
