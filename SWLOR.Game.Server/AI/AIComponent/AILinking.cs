using FluentBehaviourTree;
using SWLOR.Game.Server.AI.Contracts;

using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.Service.Contracts;
using static NWN._;
using System;
using System.Linq;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.AI.AIComponent
{
    /// <summary>
    /// This component causes the AI to link to each other.
    /// </summary>
    public class AILinking : IRegisteredEvent
    {
        
        
        
        public AILinking(
            )
        {
            
            
        }

        public bool Run(object[] args)
        {
            NWCreature self = (NWCreature)args[0];
            if (EnmityService.IsEnmityTableEmpty(self)) return false;
            float aggroRange = self.GetLocalFloat("AGGRO_RANGE");
            if (aggroRange <= 0.0f) aggroRange = 10.0f;

            int nth = 1;
            NWCreature creature = _.GetNearestObject(OBJECT_TYPE_CREATURE, self, nth);
            var target = EnmityService.GetEnmityTable(self).OrderByDescending(x => x.Value).First().Value.TargetObject;
            
            while (creature.IsValid)
            {
                if (creature.IsPlayer == false &&
                    _.GetIsEnemy(creature, self) == FALSE &&
                    !EnmityService.IsOnEnmityTable(creature, target) &&
                    _.GetDistanceBetween(self, creature) <= aggroRange &&
                    self.RacialType == creature.RacialType)
                {
                    EnmityService.AdjustEnmity(creature, target, 0, 1);
                }
                nth++;
                creature = _.GetNearestObject(OBJECT_TYPE_CREATURE, self, nth);
            }

            return true;
        }

    }
}
