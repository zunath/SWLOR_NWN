using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Event.SWLOR
{
    public class OnBaseLeaseExpired
    {
        public PCBase PCBase { get; set; }

        public OnBaseLeaseExpired(PCBase pcBase)
        {
            PCBase = pcBase;
        }
    }
}
