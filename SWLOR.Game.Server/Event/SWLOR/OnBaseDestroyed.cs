using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Event.SWLOR
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
