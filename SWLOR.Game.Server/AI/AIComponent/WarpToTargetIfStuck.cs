using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.ValueObject;
using System.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using static NWN._;

namespace SWLOR.Game.Server.AI.AIComponent
{
    /// <summary>
    /// If creature gets stuck in the same location for an extended period of time, it will teleport to the location of its target.
    /// Only happens if creature has a valid creature on its enmity table.
    /// </summary>
    public class WarpToTargetIfStuck : IRegisteredEvent
    {
        public bool Run(object[] args)
        {
            NWCreature self = (NWCreature)args[0];

            if (EnmityService.IsEnmityTableEmpty(self) ||
                _.GetMovementRate(self.Object) == 1 || // 1 = Immobile
                self.HasAnyEffect(EFFECT_TYPE_DAZED) ||  // Dazed
                self.RightHand.CustomItemType == CustomItemType.BlasterRifle ||
                self.RightHand.CustomItemType == CustomItemType.BlasterPistol)
            {
                if (self.Data.ContainsKey("WarpToTargetIfStuck_Position"))
                    self.Data.Remove("WarpToTargetIfStuck_Position");
                if (self.Data.ContainsKey("WarpToTargetIfStuck_CyclesStuckInPlace"))
                    self.Data.Remove("WarpToTargetIfStuck_CyclesStuckInPlace");

                return false;
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
                    EnmityTable table = EnmityService.GetEnmityTable(self);
                    var topTarget = table.Values.OrderByDescending(o => o.TotalAmount).FirstOrDefault();
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

            return true;
        }

    }
}
