using System.Linq;
using FluentBehaviourTree;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Processor.Contracts;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Processor
{
    public class BehaviourProcessor: IEventProcessor
    {
        private readonly IObjectProcessingService _ops;

        public BehaviourProcessor(IObjectProcessingService ops)
        {
            _ops = ops;
        }

        public void Run(object[] args)
        {
            TimeData time = new TimeData(_ops.ProcessingTickInterval);
            IBehaviourTreeNode node = (IBehaviourTreeNode)args[0];
            NWCreature creature = (NWCreature)args[1];
            bool hasPCs = NWModule.Get().Players.Count(x => x.Area.Resref == creature.Area.Resref) > 0;

            if (creature.IsValid && 
                !creature.IsDead && 
                !creature.IsPossessedFamiliar && 
                !creature.IsDMPossessed &&
                hasPCs)
            {
                node.Tick(time);
            }
        }
    }
}
