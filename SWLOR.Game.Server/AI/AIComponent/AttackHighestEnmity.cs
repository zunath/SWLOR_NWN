using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using System.Linq;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.AI.AIComponent
{
    /// <summary>
    /// This causes a creature to attack the creature with the highest enmity.
    /// </summary>
    public class AttackHighestEnmity : IRegisteredEvent
    {
        public bool Run(object[] args)
        {
            NWCreature self = (NWCreature)args[0];
            var enmityTable = EnmityService.GetEnmityTable(self);
            var target = enmityTable.Values
                .OrderByDescending(o => o.TotalAmount)
                .FirstOrDefault(x => x.TargetObject.IsValid &&
                                      x.TargetObject.Area.Equals(self.Area));

            self.AssignCommand(() =>
            {
                if (target == null)
                {
                    _.ClearAllActions();
                }
                else
                {
                    if (_.GetAttackTarget(self.Object) != target.TargetObject.Object)
                    {
                        _.ClearAllActions();
                        _.ActionAttack(target.TargetObject.Object);
                    }
                }
            });

            return true;
        }
    }
}
