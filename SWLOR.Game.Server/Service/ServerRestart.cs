namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The player side of a planned server restart. Being booted by one is not the player's choice, so it does not count as leaving.
    /// </summary>
    public static class ServerRestart
    {
        /// <summary>
        /// True from the moment a planned restart starts booting players until the server stops.
        /// </summary>
        public static bool IsRestarting { get; private set; }

        public static void BootAllPlayers(string message)
        {
            IsRestarting = true;
            StatusEffect.SaveTogglesForRestart();

            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                ExportSingleCharacter(player);
                BootPC(player, message);
            }
        }
    }
}
