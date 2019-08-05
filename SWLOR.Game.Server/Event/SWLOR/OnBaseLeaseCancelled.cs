using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Event.SWLOR
{
    public class OnBaseLeaseCancelled
    {
        public PCBase PCBase { get; set; }

        public OnBaseLeaseCancelled(PCBase pcBase)
        {
            PCBase = pcBase;
        }
    }
}
