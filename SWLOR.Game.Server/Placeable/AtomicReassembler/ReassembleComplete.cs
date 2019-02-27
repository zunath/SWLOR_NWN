using SWLOR.Game.Server.Event;

namespace SWLOR.Game.Server.Placeable.AtomicReassembler
{
    public class ReassembleComplete: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            return true;
        }
    }
}
