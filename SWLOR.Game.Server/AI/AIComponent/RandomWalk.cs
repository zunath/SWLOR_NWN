﻿using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.AI.AIComponent
{
    /// <summary>
    /// Causes creature to walk randomly every second, with a percent chance of it happening.
    /// </summary>
    public class RandomWalk : IRegisteredEvent
    {
        public bool Run(object[] args)
        {
            NWCreature self = (NWCreature)args[0];

            if (self.IsInCombat || !EnmityService.IsEnmityTableEmpty(self))
            {
                if (_.GetCurrentAction(self.Object) == _.ACTION_RANDOMWALK)
                {
                    self.ClearAllActions();
                }

                return false;
            }

            if (_.GetCurrentAction(self.Object) == _.ACTION_INVALID &&
                _.IsInConversation(self.Object) == _.FALSE &&
                _.GetCurrentAction(self.Object) != _.ACTION_RANDOMWALK &&
                RandomService.Random(100) <= 25)
            {
                self.AssignCommand(() => _.ActionRandomWalk());
            }
            return true;
        }

    }
}
