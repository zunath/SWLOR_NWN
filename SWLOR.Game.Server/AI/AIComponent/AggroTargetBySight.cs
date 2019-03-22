using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Bioware;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.Service.Contracts;
using static NWN._;

namespace SWLOR.Game.Server.AI.AIComponent
{
    /// <summary>
    /// This component causes the creature to target a player by sight.
    /// </summary>
    public class AggroTargetBySight: IRegisteredEvent
    {
        private readonly IEnmityService _enmity;
        
        public AggroTargetBySight(
            IEnmityService enmity)
        {
            _enmity = enmity;
        }

        public bool Run(object[] args)
        {
            NWCreature self = (NWCreature)args[0];

            if (self.IsInCombat) return false;

            float aggroRange = self.GetLocalFloat("AGGRO_RANGE");
            if (aggroRange <= 0.0f) aggroRange = 10.0f;
            Location targetLocation = _.Location(
                self.Area.Object,
                BiowarePosition.GetChangedPosition(self.Position, aggroRange, self.Facing),
                self.Facing + 180.0f);
            
            NWCreature creature = _.GetFirstObjectInShape(SHAPE_SPELLCYLINDER, aggroRange, targetLocation, TRUE, OBJECT_TYPE_CREATURE, self.Position);
            while (creature.IsValid)
            {
                if (_.GetIsEnemy(creature.Object, self.Object) == TRUE &&
                    !_enmity.IsOnEnmityTable(self, creature) &&
                    _.GetDistanceBetween(self.Object, creature.Object) <= aggroRange &&
                    !creature.HasAnyEffect(EFFECT_TYPE_INVISIBILITY, EFFECT_TYPE_SANCTUARY))
                {
                    _enmity.AdjustEnmity(self, creature, 0, 1);
                }
                
                creature = _.GetNextObjectInShape(SHAPE_SPELLCYLINDER, aggroRange, targetLocation, TRUE, OBJECT_TYPE_CREATURE, self.Position);
            }

            return true;
        }
    
    }
}
