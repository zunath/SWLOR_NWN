using FluentBehaviourTree;
using SWLOR.Game.Server.AI.Contracts;

namespace SWLOR.Game.Server.Extension
{
    public static class BehaviourTreeBuilderExtensions
    {
        public static BehaviourTreeBuilder Do<T>(this BehaviourTreeBuilder builder, params object[] args)
            where T: IAIComponent
        {
            var component = App.ResolveByInterface<IAIComponent>(typeof(T).ToString());
            return component.Build(builder, args);
        }
    }
}
