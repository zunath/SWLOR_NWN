using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Event.Player
{
    public class OnPlayerHeartbeat: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWCreature self = Object.OBJECT_SELF;
            SpaceService.OnCreatureHeartbeat(self);

            return true;
        }
    }
}
