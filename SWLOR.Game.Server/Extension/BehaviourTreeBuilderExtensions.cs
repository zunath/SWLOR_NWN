using FluentBehaviourTree;
using SWLOR.Game.Server.AI.Contracts;
using SWLOR.Game.Server.Event;

namespace SWLOR.Game.Server.Extension
{
    public static class BehaviourTreeBuilderExtensions
    {
        public static BehaviourTreeBuilder Do<T>(this BehaviourTreeBuilder builder, params object[] args)
            where T: IRegisteredEvent
        {
            return builder.Do(typeof(T).ToString(), t =>
            {
                bool success = App.RunEvent<T>(args);

                return success ?
                    BehaviourTreeStatus.Running :
                    BehaviourTreeStatus.Failure;
            });
            
        }
    }
}
