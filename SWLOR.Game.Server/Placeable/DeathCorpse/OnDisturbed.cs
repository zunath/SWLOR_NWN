using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using NWN;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Placeable.DeathCorpse
{
    public class OnDisturbed: IRegisteredEvent
    {
        private readonly IDeathService _death;

        public OnDisturbed(IDeathService death)
        {
            _death = death;
        }

        public bool Run(params object[] args)
        {
            _death.OnCorpseDisturb(NWPlaceable.Wrap(Object.OBJECT_SELF));
            return true;
        }
    }
}
