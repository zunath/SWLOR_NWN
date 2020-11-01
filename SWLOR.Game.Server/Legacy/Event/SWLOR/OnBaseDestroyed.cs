using SWLOR.Game.Server.Legacy.Data.Entity;
using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.Event.SWLOR
{
    public class OnBaseDestroyed
    {
        public PCBase PCBase { get; set; }
        public NWCreature LastAttacker { get; set; }

        public OnBaseDestroyed(PCBase pcBase, NWCreature lastAttacker)
        {
            PCBase = pcBase;
            LastAttacker = lastAttacker;
        }
    }
}
