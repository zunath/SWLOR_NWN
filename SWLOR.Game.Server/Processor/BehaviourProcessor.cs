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

            if (creature.IsValid && !creature.IsDead && !creature.IsPossessedFamiliar && !creature.IsDMPossessed)
            {
                node.Tick(time);
            }
        }
    }
}
