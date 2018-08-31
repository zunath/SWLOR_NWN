using FluentBehaviourTree;
using SWLOR.Game.Server.AI.Contracts;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Contracts;
using SWLOR.Game.Server.NWN.NWScript;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.AI.AIComponent
{
    public class AggroTargetBySound : IAIComponent
    {
        private readonly INWScript _;
        private readonly IEnmityService _enmity;

        public AggroTargetBySound(INWScript script,
            IEnmityService enmity)
        {
            _ = script;
            _enmity = enmity;
        }

        public BehaviourTreeBuilder Build(BehaviourTreeBuilder builder, params object[] args)
        {
            return builder.Do("AggroTargetBySound", t =>
            {
                NWCreature self = (NWCreature)args[0];

                if (self.IsInCombat) return BehaviourTreeStatus.Failure;

                float aggroRange = self.GetLocalFloat("AGGRO_RANGE");
                if (aggroRange <= 0.0f) aggroRange = 5.0f;

                int nth = 1;
                NWCreature creature = NWCreature.Wrap(_.GetNearestObject(NWScript.OBJECT_TYPE_CREATURE, self.Object, nth));
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
                    creature = NWCreature.Wrap(_.GetNearestObject(NWScript.OBJECT_TYPE_CREATURE, self.Object, nth));
                }


                return BehaviourTreeStatus.Running;
            });
        }
    }
}
