using FluentBehaviourTree;
using SWLOR.Game.Server.AI.Contracts;
using SWLOR.Game.Server.GameObject;

using NWN;

namespace SWLOR.Game.Server.AI.AIComponent
{
    /// <summary>
    /// Forces creature to equip the best ranged weapon they have.
    /// </summary>
    public class EquipBestRanged : IAIComponent
    {
        private readonly INWScript _;

        public EquipBestRanged(INWScript script)
        {
            _ = script;
        }

        public BehaviourTreeBuilder Build(BehaviourTreeBuilder builder, params object[] args)
        {
            return builder.Do("EquipBestRanged", t =>
            {
                NWCreature self = (NWCreature)args[0];
                if (!self.IsInCombat ||
                    !self.RightHand.IsRanged)
                    return BehaviourTreeStatus.Failure;

                self.AssignCommand(() =>
                {
                    _.ActionEquipMostDamagingRanged(new Object());
                });

                return BehaviourTreeStatus.Running;
            });
        }
    }
}
