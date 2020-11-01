namespace SWLOR.Game.Server.Legacy.Event.SWLOR
{
    public class OnAreaInstanceCreated
    {
        public uint Instance { get; set; }

        public OnAreaInstanceCreated(uint instance)
        {
            Instance = instance;
        }
    }
}
