using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.Service;

using static SWLOR.Game.Server.NWN._;

namespace SWLOR.Game.Server.AI
{
    /// <summary>
    /// Standard behaviour which executes for all derived behaviours
    /// </summary>
    public class StarshipBehaviour : BehaviourBase
    {
        public override bool IgnoreNWNEvents => true;
        
        public override void OnPhysicalAttacked(NWCreature self)
        {
            base.OnPhysicalAttacked(self);
            NWCreature attacker = _.GetLastAttacker();
            SpaceService.OnPhysicalAttacked(self, attacker);
        }

        public override void OnPerception(NWCreature self)
        {
            base.OnPerception(self);
            SpaceService.OnPerception(_.OBJECT_SELF, _.GetLastPerceived());
        }

        public override void OnHeartbeat(NWCreature self)
        {
            base.OnHeartbeat(self);
            SpaceService.OnHeartbeat(_.OBJECT_SELF);
        }
    }
}
