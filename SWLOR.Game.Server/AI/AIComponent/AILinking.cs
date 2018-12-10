using FluentBehaviourTree;
using SWLOR.Game.Server.AI.Contracts;
using SWLOR.Game.Server.Bioware.Contracts;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;
using System;

namespace SWLOR.Game.Server.AI.AIComponent
{
    /// <summary>
    /// This component causes the AI to link to each other.
    /// </summary>
    public class AILinking : IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IEnmityService _enmity;
        private readonly IBiowarePosition _biowarePos;

        public AILinking(INWScript script,
            IEnmityService enmity,
            IBiowarePosition biowarePos)
        {
            _ = script;
            _enmity = enmity;
            _biowarePos = biowarePos;
        }

        public bool Run(object[] args)
        {
            NWCreature self = (NWCreature)args[0];

            if (!self.IsInCombat) return false;
            float aggroRange = self.GetLocalFloat("AGGRO_RANGE");
            if (aggroRange <= 0.0f) aggroRange = 30.0f;
            Location targetLocation = _.Location(
                self.Area.Object,
                _biowarePos.GetChangedPosition(self.Position, aggroRange, self.Facing),
                self.Facing + 180.0f);
            int nth = 1;
            NWCreature creature = _.GetNearestObject(OBJECT_TYPE_CREATURE, self.Object, nth);
            while (creature.IsValid && !creature.IsDead)
            {
                if (_.GetIsEnemy(creature.Object, self.Object) == FALSE &&
                    !_enmity.IsOnEnmityTable(self, creature) &&
                    _.GetDistanceBetween(self.Object, creature.Object) <= aggroRange &&
                    self.RacialType == creature.RacialType &&
                    creature.IsPlayer == false)
                {
                    _enmity.AdjustEnmity(creature, _.GetAttackTarget(self), 0, 1);
                    _.SendMessageToPC(_.GetAttackTarget(self), _.GetName(creature) + " #" + nth.ToString() + " is attacking " + _.GetName(_.GetAttackTarget(self.Object)) + " !");

                }
                nth++;
                creature = _.GetNearestObject(OBJECT_TYPE_CREATURE, self.Object, nth);
            }

            return true;
        }

    }
}
