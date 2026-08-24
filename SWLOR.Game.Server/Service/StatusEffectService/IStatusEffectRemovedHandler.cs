namespace SWLOR.Game.Server.Service.StatusEffectService
{
    /// <summary>
    /// Receives a callback after a status effect has been removed from the creature's tracked
    /// effect collection. Use this when reconciliation must observe the post-removal state.
    /// </summary>
    public interface IStatusEffectRemovedHandler
    {
        void AfterRemoved(uint creature);
    }
}
