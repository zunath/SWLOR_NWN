using FluentBehaviourTree;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IBehaviourService
    {
        void RegisterBehaviour(IBehaviourTreeNode node, NWCreature creature);
    }
}