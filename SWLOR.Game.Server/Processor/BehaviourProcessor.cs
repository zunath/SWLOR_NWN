using System;
using System.Linq;
using FluentBehaviourTree;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Processor.Contracts;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Processor
{
    public class BehaviourProcessor: IEventProcessor
    {
        private readonly IBehaviourTreeNode _node;
        private readonly NWCreature _creature;

        public BehaviourProcessor(IBehaviourTreeNode node, NWCreature creature)
        {
            _node = node;
            _creature = creature;
        }

        public void Run()
        {
            TimeData time = new TimeData(ObjectProcessingService.ProcessingTickInterval);
            
            bool hasPCs = NWModule.Get().Players.Count(x => x.Area.Resref == _creature.Area.Resref) > 0;

            if (_creature.IsValid && 
                !_creature.IsDead && 
                !_creature.IsPossessedFamiliar && 
                !_creature.IsDMPossessed &&
                hasPCs)
            {
                _node.Tick(time);
            }
        }
    }
}
