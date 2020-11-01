namespace SWLOR.Game.Server.Legacy.Event.SWLOR
{
    public class OnAreaInstanceDestroyed
    {
        public uint Instance { get; set; }

        public OnAreaInstanceDestroyed(uint instance)
        {
            Instance = instance;
        }
    }
}
