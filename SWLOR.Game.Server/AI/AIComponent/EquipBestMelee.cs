using FluentBehaviourTree;
using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.AI.AIComponent
{
    /// <summary>
    /// Forces creature to equip the best melee item.
    /// </summary>
    public class EquipBestMelee : IRegisteredEvent
    {
        public bool Run(object[] args)
        {
            NWCreature self = (NWCreature)args[0];
            if (!self.IsInCombat ||
                 self.RightHand.IsRanged)
                return false;

            self.AssignCommand(() =>
            {
                _.ActionEquipMostDamagingMelee(new Object());
            });

            return true;
        }

    }
}
