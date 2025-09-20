using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Shared.Events.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ScriptHandlerAttribute : Attribute
    {
        /// <summary>
        /// The script name this method handles.
        /// </summary>
        public string Script { get; }

        /// <summary>
        /// Initializes a new instance of the ScriptHandlerAttribute.
        /// </summary>
        /// <param name="script">The script name this method handles</param>
        public ScriptHandlerAttribute(string script)
        {
            Script = script ?? throw new ArgumentNullException(nameof(script));
        }
    }

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
