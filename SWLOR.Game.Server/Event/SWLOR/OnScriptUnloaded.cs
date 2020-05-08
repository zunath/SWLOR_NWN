namespace SWLOR.Game.Server.Scripting
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
