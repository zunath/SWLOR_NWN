namespace SWLOR.Shared.Events.Attributes
{
    /// <summary>
    /// Legacy attribute for marking methods as script handlers for backwards compatibility.
    /// This provides a migration path from the old script-based event system.
    /// </summary>
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
}
