using FluentBehaviourTree;

namespace SWLOR.Game.Server.AI.Contracts
{
    public interface IAIComponent
    {
        BehaviourTreeBuilder Build(BehaviourTreeBuilder builder, params object[] args);
    }
}
