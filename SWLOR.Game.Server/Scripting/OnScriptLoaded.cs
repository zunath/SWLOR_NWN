using SWLOR.Game.Server.Scripting.Contracts;

namespace SWLOR.Game.Server.Scripting
{
    public class OnScriptLoaded
    {
        public IScript Script { get; set; }

        public OnScriptLoaded(IScript script)
        {
            Script = script;
        }
    }
}
