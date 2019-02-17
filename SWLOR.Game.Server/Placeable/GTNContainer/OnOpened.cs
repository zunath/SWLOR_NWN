using SWLOR.Game.Server.Event;

namespace SWLOR.Game.Server.Placeable.GTNContainer
{
    public class OnOpened: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            return true;
        }
    }
}
