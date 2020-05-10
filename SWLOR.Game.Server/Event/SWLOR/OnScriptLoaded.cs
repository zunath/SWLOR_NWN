using SWLOR.Game.Server.Scripts;

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
