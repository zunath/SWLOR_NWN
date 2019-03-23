using FluentBehaviourTree;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Processor;


namespace SWLOR.Game.Server.Service
{
    public static class BehaviourService
    {
        public static void RegisterBehaviour(IBehaviourTreeNode node, NWCreature creature)
        {
            string behaviourID = ObjectProcessingService.RegisterProcessingEvent<BehaviourProcessor>(node, creature);
            AppCache.NPCBehaviours.Add(behaviourID, creature);
        }
        
    }
}
