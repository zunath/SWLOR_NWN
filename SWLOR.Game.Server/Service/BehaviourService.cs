using FluentBehaviourTree;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Processor;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Service
{
    public class BehaviourService : IBehaviourService
    {
        private readonly IObjectProcessingService _objProc;
        
        public BehaviourService(IObjectProcessingService objProc)
        {
            _objProc = objProc;
        }
        
        public void RegisterBehaviour(IBehaviourTreeNode node, NWCreature creature)
        {
            string behaviourID = _objProc.RegisterProcessingEvent<BehaviourProcessor>(node, creature);
            AppCache.NPCBehaviours.Add(behaviourID, creature);
        }
        
    }
}
