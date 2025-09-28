using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Shared.Events.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ScriptHandlerAttribute<T> : Attribute
        where T: IEvent
    {
        /// <summary>
        /// Initializes a new instance of the ScriptHandlerAttribute.
        /// The script name will be automatically derived from the event type.
        /// </summary>
        public ScriptHandlerAttribute()
        {
        }
    }
}
