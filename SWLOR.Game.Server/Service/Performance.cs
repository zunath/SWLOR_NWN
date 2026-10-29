namespace SWLOR.Game.Server.Service
{
    public static class Performance
    {
        public static void ToggleDance(uint activator, int level)
        {
            SendMessageToPC(activator, "Performances are coming soon.");
        }

        public static void TogglePlayInstrument(uint activator, int level)
        {
            SendMessageToPC(activator, "Performances are coming soon.");
        }
    }
}
