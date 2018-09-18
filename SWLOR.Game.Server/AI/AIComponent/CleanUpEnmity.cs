using System.Linq;
using FluentBehaviourTree;
using NWN;
using SWLOR.Game.Server.AI.Contracts;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.AI.AIComponent
{
    /// <summary>
    /// This cleans up enmity, removing dead objects from the enmity table.
    /// </summary>
    public class CleanUpEnmity: IAIComponent
    {
        private readonly INWScript _;
        private readonly IEnmityService _enmity;
        
        public CleanUpEnmity(INWScript script,
            IEnmityService enmity)
        {
            _ = script;
            _enmity = enmity;
        }

        public BehaviourTreeBuilder Build(BehaviourTreeBuilder builder, params object[] args)
        {
            NWCreature self = (NWCreature) args[0];

            return builder.Do("CleanUpEnmity", t =>
            {
                foreach (var enmity in _enmity.GetEnmityTable(self).ToArray())
                {
                    var val = enmity.Value;
                    var target = val.TargetObject;

                    // Remove invalid objects from the enmity table
                    if (target == null ||
                        !target.IsValid ||
                        !target.Area.Equals(self.Area) ||
                        target.CurrentHP <= -11)
                    {
                        _enmity.GetEnmityTable(self).Remove(enmity.Key);
                        continue;
                    }
                    
                    _.AdjustReputation(target.Object, self.Object, -100);

                    // Reduce volatile enmity every tick
                    _enmity.GetEnmityTable(self)[target.GlobalID].VolatileAmount--;
                }

                return BehaviourTreeStatus.Running;
            });
        }
    }
}
