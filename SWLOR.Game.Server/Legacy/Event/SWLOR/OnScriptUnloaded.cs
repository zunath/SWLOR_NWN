namespace SWLOR.Game.Server.Legacy.Event.SWLOR
{
    public class OnScriptUnloaded
    {
        public string File { get; set; }

        public OnScriptUnloaded(string file)
        {
            File = file;
        }
    }
}
