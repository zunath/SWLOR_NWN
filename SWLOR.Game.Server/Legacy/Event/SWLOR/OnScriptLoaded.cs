using SWLOR.Game.Server.Legacy.Scripts;

namespace SWLOR.Game.Server.Legacy.Event.SWLOR
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
