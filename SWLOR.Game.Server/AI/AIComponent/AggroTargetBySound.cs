using FluentBehaviourTree;
using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.AI.AIComponent
{
    /// <summary>
    /// This component causes a creature to target by sound (i.e: what's near them)
    /// </summary>
    public class AggroTargetBySound : IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IEnmityService _enmity;

        public AggroTargetBySound(INWScript script,
            IEnmityService enmity)
        {
            _ = script;
            _enmity = enmity;
        }

        public bool Run(object[] args)
        {
            NWCreature self = (NWCreature)args[0];

            if (self.IsInCombat) return false;

            float aggroRange = self.GetLocalFloat("AGGRO_RANGE");
            if (aggroRange <= 0.0f) aggroRange = 5.0f;

            int nth = 1;
            NWCreature creature = _.GetNearestObject(NWScript.OBJECT_TYPE_CREATURE, self.Object, nth);
            while (creature.IsValid)
            {
                if (_.GetIsEnemy(creature.Object, self.Object) == NWScript.TRUE &&
                    !_enmity.IsOnEnmityTable(self, creature) &&
                    !creature.HasAnyEffect(NWScript.EFFECT_TYPE_SANCTUARY) &&
                    _.GetDistanceBetween(self.Object, creature.Object) <= aggroRange &&
                    _.LineOfSightObject(self.Object, creature.Object) == NWScript.TRUE)
                {
                    _enmity.AdjustEnmity(self, creature, 0, 1);
                }

                nth++;
                creature = _.GetNearestObject(NWScript.OBJECT_TYPE_CREATURE, self.Object, nth);
            }


            return true;

        }
    }
}
