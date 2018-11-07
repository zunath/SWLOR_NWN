using FluentBehaviourTree;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Processor;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Service
{
    public class BehaviourService : IBehaviourService
    {
        private readonly IObjectProcessingService _objProc;
        private readonly AppCache _cache;

        public BehaviourService(IObjectProcessingService objProc,
            AppCache cache)
        {
            _objProc = objProc;
            _cache = cache;
        }
        
        public void RegisterBehaviour(IBehaviourTreeNode node, NWCreature creature)
        {
            string behaviourID = _objProc.RegisterProcessingEvent<BehaviourProcessor>(node, creature);
            _cache.NPCBehaviours.Add(behaviourID, creature);
        }
        
    }
}
