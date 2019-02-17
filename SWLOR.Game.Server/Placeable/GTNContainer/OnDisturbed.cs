using SWLOR.Game.Server.Event;

namespace SWLOR.Game.Server.Placeable.GTNContainer
{
    public class OnDisturbed: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            return true;
        }
    }
}
