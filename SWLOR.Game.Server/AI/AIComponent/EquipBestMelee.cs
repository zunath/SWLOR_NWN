using FluentBehaviourTree;
using SWLOR.Game.Server.AI.Contracts;
using SWLOR.Game.Server.GameObject;

using NWN;

namespace SWLOR.Game.Server.AI.AIComponent
{
    /// <summary>
    /// Forces creature to equip the best melee item.
    /// </summary>
    public class EquipBestMelee: IAIComponent
    {
        private readonly INWScript _;

        public EquipBestMelee(INWScript script)
        {
            _ = script;
        }

        public BehaviourTreeBuilder Build(BehaviourTreeBuilder builder, params object[] args)
        {
            return builder.Do("EquipBestMelee", t =>
            {
                NWCreature self = (NWCreature)args[0];
                if (!self.IsInCombat ||
                     self.RightHand.IsRanged)
                    return BehaviourTreeStatus.Failure;

                self.AssignCommand(() =>
                {
                    _.ActionEquipMostDamagingMelee(new Object());
                });
                
                return BehaviourTreeStatus.Running;
            });
        }
    }
}
