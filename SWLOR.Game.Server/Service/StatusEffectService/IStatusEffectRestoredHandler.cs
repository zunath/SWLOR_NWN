namespace SWLOR.Game.Server.Service.StatusEffectService
{
    /// <summary>
    /// Receives a callback after a persisted status effect has been restored to the creature's
    /// tracked effect collection. Use this to rebuild derived state from the restored effects.
    /// </summary>
    public interface IStatusEffectRestoredHandler
    {
        void AfterRestored(uint creature);
    }
}
