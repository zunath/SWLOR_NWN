using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.NWScript;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Player
{
    public class OnPlayerHeartbeat: IRegisteredEvent
    {
        private readonly ICustomEffectService _customEffect;

        public OnPlayerHeartbeat(ICustomEffectService customEffect)
        {
            _customEffect = customEffect;
        }

        public bool Run(params object[] args)
        {
            NWPlayer player = NWPlayer.Wrap(Object.OBJECT_SELF);
            _customEffect.OnPlayerHeartbeat(player);
            return true;
        }
    }
}
