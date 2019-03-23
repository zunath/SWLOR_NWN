using System;
using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using System.Linq;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.AI.AIComponent
{
    /// <summary>
    /// This cleans up enmity, removing dead objects from the enmity table.
    /// </summary>
    public class CleanUpEnmity : IRegisteredEvent
    {
        
        

        public CleanUpEnmity(
            )
        {
            
            
        }

        public bool Run(object[] args)
        {
            NWCreature self = (NWCreature)args[0];
            var table = EnmityService.GetEnmityTable(self);
            if (table.Count <= 0) return false;

            foreach (var enmity in table.ToArray())
            {
                var val = enmity.Value;
                var target = val.TargetObject;

                // Remove invalid objects from the enmity table
                if (target == null ||
                    !target.IsValid ||
                    !target.Area.Equals(self.Area) ||
                    target.CurrentHP <= -11)
                {
                    EnmityService.GetEnmityTable(self).Remove(enmity.Key);
                    continue;
                }

                _.AdjustReputation(target.Object, self.Object, -100);

                // Reduce volatile enmity every tick
                EnmityService.GetEnmityTable(self)[target.GlobalID].VolatileAmount--;
            }

            return true;
        }

    }
}
