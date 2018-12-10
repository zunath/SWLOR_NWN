using FluentBehaviourTree;
using SWLOR.Game.Server.AI.Contracts;
using SWLOR.Game.Server.Bioware.Contracts;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;
using System;
using System.Linq;

namespace SWLOR.Game.Server.AI.AIComponent
{
    /// <summary>
    /// This component causes the AI to link to each other.
    /// </summary>
    public class AILinking : IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IEnmityService _enmity;
        
        public AILinking(INWScript script,
            IEnmityService enmity)
        {
            _ = script;
            _enmity = enmity;
        }

        public bool Run(object[] args)
        {
            NWCreature self = (NWCreature)args[0];
            if (_enmity.IsEnmityTableEmpty(self)) return false;
            float aggroRange = self.GetLocalFloat("AGGRO_RANGE");
            if (aggroRange <= 0.0f) aggroRange = 10.0f;

            int nth = 1;
            NWCreature creature = _.GetNearestObject(OBJECT_TYPE_CREATURE, self, nth);
            var target = _enmity.GetEnmityTable(self).OrderByDescending(x => x.Value).First().Value.TargetObject;
            
            while (creature.IsValid)
            {
                if (creature.IsPlayer == false &&
                    _.GetIsEnemy(creature, self) == FALSE &&
                    !_enmity.IsOnEnmityTable(creature, target) &&
                    _.GetDistanceBetween(self, creature) <= aggroRange &&
                    self.RacialType == creature.RacialType)
                {
                    _enmity.AdjustEnmity(creature, target, 0, 1);
                }
                nth++;
                creature = _.GetNearestObject(OBJECT_TYPE_CREATURE, self, nth);
            }

            return true;
        }

    }
}
