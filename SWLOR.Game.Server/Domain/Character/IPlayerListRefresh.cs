namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The player list refresh request the holocom and the name service make. Character owns the interface and
    /// <c>IPlayerListService</c> extends it (supply rule 2 of MaintainabilityRefactorDomainOrder.md; P5-15a), so the callers
    /// below Social need nothing from it.
    /// </summary>
    public interface IPlayerListRefresh
    {
        /// <summary>
        /// Refreshes every open player list on the next frame. Repeated requests in one frame refresh once.
        /// </summary>
        void QueueRefresh();
    }
}
