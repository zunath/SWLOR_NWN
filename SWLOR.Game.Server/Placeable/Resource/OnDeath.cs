using SWLOR.Game.Server.Event;

namespace SWLOR.Game.Server.Placeable.Resource
{
    public class OnDeath: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            return true;
        }
    }
}
