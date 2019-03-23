using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Player
{
    public class OnPlayerDamaged : IRegisteredEvent
    {
        

        public OnPlayerDamaged()
        {
            
        }

        public bool Run(params object[] args)
        {
            EnmityService.OnPlayerDamaged();
            return true;
        }
    }
}
