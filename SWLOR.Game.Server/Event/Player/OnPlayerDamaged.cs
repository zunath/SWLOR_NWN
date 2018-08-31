using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Player
{
    public class OnPlayerDamaged : IRegisteredEvent
    {
        private readonly IEnmityService _enmity;

        public OnPlayerDamaged(IEnmityService enmity)
        {
            _enmity = enmity;
        }

        public bool Run(params object[] args)
        {
            _enmity.OnPlayerDamaged();
            return true;
        }
    }
}
