using System;
using FluentBehaviourTree;
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
                IRegisteredEvent @event = Activator.CreateInstance<T>();
                bool success = @event.Run(args);

                return success ?
                    BehaviourTreeStatus.Running :
                    BehaviourTreeStatus.Failure;
            });
            
        }
    }
}
