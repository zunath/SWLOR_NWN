using SWLOR.Game.Server.Legacy.Data.Entity;

namespace SWLOR.Game.Server.Legacy.Event.SWLOR
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
