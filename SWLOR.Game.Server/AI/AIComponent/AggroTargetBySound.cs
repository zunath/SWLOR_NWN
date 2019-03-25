using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using static NWN._;

namespace SWLOR.Game.Server.AI.AIComponent
{
    /// <summary>
    /// This component causes a creature to target by sound (i.e: what's near them)
    /// </summary>
    public class AggroTargetBySound : IRegisteredEvent
    {
        public bool Run(object[] args)
        {
            NWCreature self = (NWCreature)args[0];

            if (self.IsInCombat) return false;

            float aggroRange = self.GetLocalFloat("AGGRO_RANGE");
            if (aggroRange <= 0.0f) aggroRange = 10.0f;

            int nth = 1;
            NWCreature creature = _.GetNearestObject(OBJECT_TYPE_CREATURE, self.Object, nth);
            while (creature.IsValid)
            {
                if (_.GetIsEnemy(creature.Object, self.Object) == TRUE &&
                    !EnmityService.IsOnEnmityTable(self, creature) &&
                    !creature.HasAnyEffect(EFFECT_TYPE_SANCTUARY) &&
                    _.GetDistanceBetween(self.Object, creature.Object) <= aggroRange &&
                    _.LineOfSightObject(self.Object, creature.Object) == TRUE)
                {
                    EnmityService.AdjustEnmity(self, creature, 0, 1);
                }

                nth++;
                creature = _.GetNearestObject(OBJECT_TYPE_CREATURE, self.Object, nth);
            }


            return true;

        }
    }
}
