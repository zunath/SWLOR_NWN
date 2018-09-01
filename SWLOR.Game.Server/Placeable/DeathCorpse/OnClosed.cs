using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using NWN;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Placeable.DeathCorpse
{
    public class OnClosed: IRegisteredEvent
    {
        private readonly IDeathService _death;

        public OnClosed(IDeathService death)
        {
            _death = death;
        }

        public bool Run(params object[] args)
        {
            _death.OnCorpseClose(NWPlaceable.Wrap(Object.OBJECT_SELF));
            return true;
        }
    }
}
