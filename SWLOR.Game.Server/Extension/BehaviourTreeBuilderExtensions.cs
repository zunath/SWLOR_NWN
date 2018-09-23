using FluentBehaviourTree;
using SWLOR.Game.Server.AI.Contracts;

namespace SWLOR.Game.Server.Extension
{
    public static class BehaviourTreeBuilderExtensions
    {
        public static BehaviourTreeBuilder Do<T>(this BehaviourTreeBuilder builder, params object[] args)
            where T: IAIComponent
        {
            return App.ResolveByInterface<IAIComponent, BehaviourTreeBuilder>(typeof(T).ToString(), component => component.Build(builder, args));
        }
    }
}
