namespace SWLOR.Shared.Core.Event
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ScriptHandler : Attribute
    {
        public string Script { get; }

        public ScriptHandler(string script)
        {
            Script = script;
        }

    }
}