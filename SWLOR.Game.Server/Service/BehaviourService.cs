using FluentBehaviourTree;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Processor;


namespace SWLOR.Game.Server.Service
{
    public static class BehaviourService
    {
        public static void RegisterBehaviour(IBehaviourTreeNode node, NWCreature creature)
        {
            var processor = new BehaviourProcessor(node, creature);
            string behaviourID = ObjectProcessingService.RegisterProcessingEvent(processor);
            AppCache.NPCBehaviours.Add(behaviourID, creature);
        }
        
    }
}
