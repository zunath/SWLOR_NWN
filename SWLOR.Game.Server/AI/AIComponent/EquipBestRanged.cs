using FluentBehaviourTree;
using SWLOR.Game.Server.AI.Contracts;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Contracts;
using SWLOR.Game.Server.NWN.NWScript;

namespace SWLOR.Game.Server.AI.AIComponent
{
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
