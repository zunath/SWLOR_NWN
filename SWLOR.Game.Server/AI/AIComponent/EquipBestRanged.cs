using FluentBehaviourTree;
using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.AI.AIComponent
{
    /// <summary>
    /// Forces creature to equip the best ranged weapon they have.
    /// </summary>
    public class EquipBestRanged : IRegisteredEvent
    {
        private readonly INWScript _;

        public EquipBestRanged(INWScript script)
        {
            _ = script;
        }

        public bool Run(object[] args)
        {
            NWCreature self = (NWCreature)args[0];
            if (!self.IsInCombat ||
                !self.RightHand.IsRanged)
                return false;

            self.AssignCommand(() =>
            {
                _.ActionEquipMostDamagingRanged(new Object());
            });

            return true;
        }

    }
}
