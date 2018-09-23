using FluentBehaviourTree;
using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.AI.AIComponent
{
    /// <summary>
    /// Causes creature to walk randomly every second, with a percent chance of it happening.
    /// </summary>
    public class RandomWalk : IRegisteredEvent
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

        public bool Run(object[] args)
        {
            NWCreature self = (NWCreature)args[0];

            if (self.IsInCombat || !_enmity.IsEnmityTableEmpty(self))
            {
                if (_.GetCurrentAction(self.Object) == NWScript.ACTION_RANDOMWALK)
                {
                    self.ClearAllActions();
                }

                return false;
            }

            if (_.GetCurrentAction(self.Object) == NWScript.ACTION_INVALID &&
                _.IsInConversation(self.Object) == NWScript.FALSE &&
                _.GetCurrentAction(self.Object) != NWScript.ACTION_RANDOMWALK &&
                _random.Random(100) <= 25)
            {
                self.AssignCommand(() => _.ActionRandomWalk());
            }
            return true;
        }

    }
}
