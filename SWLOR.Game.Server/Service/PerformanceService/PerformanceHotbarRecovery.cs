namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>Reconciles a saved Performance hot bar with the checkpoint of the character file that loaded.</summary>
    public static class PerformanceHotbarRecovery
    {
        public static PerformanceHotbarRecoveryAction Decide(string savedHotbar, int restoreSequence, int loadedSequence, bool atLogin)
        {
            if (savedHotbar == null)
                return PerformanceHotbarRecoveryAction.None;

            if (restoreSequence <= 0)
                return PerformanceHotbarRecoveryAction.Restore;

            // A stopped show's bar is already restored in memory; another show must capture any edits made since.
            if (!atLogin)
                return PerformanceHotbarRecoveryAction.None;

            return loadedSequence >= restoreSequence
                ? PerformanceHotbarRecoveryAction.Forget
                : PerformanceHotbarRecoveryAction.Restore;
        }
    }
}
