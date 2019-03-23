using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Event.Player
{
    public class OnPlayerDamaged : IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            EnmityService.OnPlayerDamaged();
            return true;
        }
    }
}
