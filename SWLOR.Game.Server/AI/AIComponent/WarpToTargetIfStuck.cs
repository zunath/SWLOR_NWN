using System.Linq;
using FluentBehaviourTree;
using SWLOR.Game.Server.AI.Contracts;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.AI.AIComponent
{
    public class WarpToTargetIfStuck: IAIComponent
    {
        private readonly INWScript _;
        private readonly IEnmityService _enmity;

        public WarpToTargetIfStuck(INWScript script,
            IEnmityService enmity)
        {
            _ = script;
            _enmity = enmity;
        }

        public BehaviourTreeBuilder Build(BehaviourTreeBuilder builder, params object[] args)
        {
            NWCreature self = (NWCreature) args[0];
            return builder.Do("WarpToTargetIfStuck", t =>
            {
                if (_enmity.IsEnmityTableEmpty(self) ||
                    _.GetMovementRate(self.Object) == 1) // 1 = Immobile
                {
                    if (self.Data.ContainsKey("WarpToTargetIfStuck_Position"))
                        self.Data.Remove("WarpToTargetIfStuck_Position");
                    if (self.Data.ContainsKey("WarpToTargetIfStuck_CyclesStuckInPlace"))
                        self.Data.Remove("WarpToTargetIfStuck_CyclesStuckInPlace");

                    return BehaviourTreeStatus.Failure;
                }
                    
                
                Vector previousPosition = new Vector();
                if (self.Data.ContainsKey("WarpToTargetIfStuck_Position"))
                    previousPosition = (Vector)self.Data["WarpToTargetIfStuck_Position"];
                
                Vector currentPosition = self.Position;
                if (previousPosition.m_X == currentPosition.m_X &&
                    previousPosition.m_Y == currentPosition.m_Y &&
                    previousPosition.m_Z == currentPosition.m_Z)
                {
                    var cyclesStuck = 0;
                    if (self.Data.ContainsKey("WarpToTargetIfStuck_CyclesStuckInPlace"))
                    {
                        cyclesStuck = (int)self.Data["WarpToTargetIfStuck_CyclesStuckInPlace"];
                    }
                    cyclesStuck++;

                    if (cyclesStuck >= 12) // Stuck for 12 seconds - warp to the target if still in the area.
                    {
                        EnmityTable table = _enmity.GetEnmityTable(self);
                        var topTarget = table.Values.OrderByDescending(o => o.TotalAmount).SingleOrDefault();
                        if (topTarget != null && topTarget.TargetObject.IsValid)
                        {
                            var location = topTarget.TargetObject.Location;
                            _.AssignCommand(self.Object, () => _.JumpToLocation(location));
                        }

                        cyclesStuck = 0;
                    }
                    
                    self.Data["WarpToTargetIfStuck_CyclesStuckInPlace"] = cyclesStuck;
                }
                else
                {
                    self.Data["WarpToTargetIfStuck_CyclesStuckInPlace"] = 0;
                }

                self.Data["WarpToTargetIfStuck_Position"] = currentPosition;

                return BehaviourTreeStatus.Running;
            });
        }
    }
}
