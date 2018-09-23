using FluentBehaviourTree;
using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using System.Linq;

namespace SWLOR.Game.Server.AI.AIComponent
{
    /// <summary>
    /// This causes a creature to attack the creature with the highest enmity.
    /// </summary>
    public class AttackHighestEnmity : IRegisteredEvent
    {
        private readonly IEnmityService _enmity;
        private readonly INWScript _;

        public AttackHighestEnmity(IEnmityService enmity,
            INWScript script)
        {
            _enmity = enmity;
            _ = script;
        }

        public bool Run(object[] args)
        {
            NWCreature self = (NWCreature)args[0];
            var enmityTable = _enmity.GetEnmityTable(self);
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
