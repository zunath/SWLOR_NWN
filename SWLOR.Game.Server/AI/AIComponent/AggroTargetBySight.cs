using FluentBehaviourTree;
using SWLOR.Game.Server.AI.Contracts;
using SWLOR.Game.Server.Bioware.Contracts;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Contracts;
using SWLOR.Game.Server.NWN.NWScript;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.AI.AIComponent
{
    public class AggroTargetBySight: IAIComponent
    {
        private readonly INWScript _;
        private readonly IEnmityService _enmity;
        private readonly IBiowarePosition _biowarePos;

        public AggroTargetBySight(INWScript script,
            IEnmityService enmity,
            IBiowarePosition biowarePos)
        {
            _ = script;
            _enmity = enmity;
            _biowarePos = biowarePos;
        }

        public BehaviourTreeBuilder Build(BehaviourTreeBuilder builder, params object[] args)
        {
            return builder.Do("AggroTargetBySight", t =>
            {
                NWCreature self = (NWCreature)args[0];

                if (self.IsInCombat) return BehaviourTreeStatus.Failure;

                float aggroRange = self.GetLocalFloat("AGGRO_RANGE");
                if (aggroRange <= 0.0f) aggroRange = 10.0f;
                Location targetLocation = _.Location(
                    self.Area.Object,
                    _biowarePos.GetChangedPosition(self.Position, aggroRange, self.Facing),
                    self.Facing + 180.0f);
                
                NWCreature creature = NWCreature.Wrap(_.GetFirstObjectInShape(NWScript.SHAPE_SPELLCYLINDER, aggroRange, targetLocation, NWScript.TRUE, NWScript.OBJECT_TYPE_CREATURE, self.Position));
                while (creature.IsValid)
                {
                    if (_.GetIsEnemy(creature.Object, self.Object) == NWScript.TRUE &&
                        !_enmity.IsOnEnmityTable(self, creature) &&
                        _.GetDistanceBetween(self.Object, creature.Object) <= aggroRange &&
                        !creature.HasAnyEffect(NWScript.EFFECT_TYPE_INVISIBILITY, NWScript.EFFECT_TYPE_SANCTUARY))
                    {
                        _enmity.AdjustEnmity(self, creature, 0, 1);
                    }
                    
                    creature = NWCreature.Wrap(_.GetNextObjectInShape(NWScript.SHAPE_SPELLCYLINDER, aggroRange, targetLocation, NWScript.TRUE, NWScript.OBJECT_TYPE_CREATURE, self.Position));
                }

                return BehaviourTreeStatus.Running;
            });
        }
    }
}
