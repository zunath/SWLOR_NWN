using SWLOR.Game.Server.Legacy.Data.Entity;

namespace SWLOR.Game.Server.Legacy.Event.SWLOR
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
