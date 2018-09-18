using FluentBehaviourTree;
using SWLOR.Game.Server.AI.Contracts;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.AI.AIComponent
{
    /// <summary>
    /// Causes creature to walk randomly every second, with a percent chance of it happening.
    /// </summary>
    public class RandomWalk: IAIComponent
    {
        private readonly INWScript _;
        private readonly IEnmityService _enmity;
        private readonly IRandomService _random;

        public RandomWalk(INWScript script,
            IEnmityService enmity,
            IRandomService random)
        {
            _ = script;
            _enmity = enmity;
            _random = random;
        }

        public BehaviourTreeBuilder Build(BehaviourTreeBuilder builder, params object[] args)
        {
            NWCreature self = (NWCreature) args[0];

            return builder.Do("RandomWalk", t =>
            {
                if (self.IsInCombat || !_enmity.IsEnmityTableEmpty(self))
                {
                    if (_.GetCurrentAction(self.Object) == NWScript.ACTION_RANDOMWALK)
                    {
                        self.ClearAllActions();
                    }

                    return BehaviourTreeStatus.Failure;
                }
                
                if (_.GetCurrentAction(self.Object) == NWScript.ACTION_INVALID &&
                    _.IsInConversation(self.Object) == NWScript.FALSE &&
                    _.GetCurrentAction(self.Object) != NWScript.ACTION_RANDOMWALK &&
                    _random.Random(100) <= 25)
                {
                    self.AssignCommand(() => _.ActionRandomWalk());
                }
                return BehaviourTreeStatus.Running;
            });
        }
    }
}
