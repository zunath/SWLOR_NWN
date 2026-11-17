namespace SWLOR.Game.Server.Domain.Companions
{
    /// <summary>
    /// Queues the tint refreshes that follow a spawn, an area entry and an equipment change; the event handlers are
    /// on <see cref="TintMapService"/>. Static callers reach the container's instance through
    /// <c>GameServices.TintMap</c>.
    /// </summary>
    public interface ITintMapService
    {
        /// <summary>
        /// Queues a refresh of a creature's colours after a short delay, recovering any robe, neck or part the
        /// wearer's body cannot show first.
        /// </summary>
        /// <param name="creature">The creature to refresh.</param>
        /// <param name="resetShaderOverrides">True to clear the creature's shader overrides first.</param>
        void QueueRefresh(uint creature, bool resetShaderOverrides = true);

        /// <summary>Queues a refresh of the colours of an item lying in an area.</summary>
        void QueueItemRefresh(uint item);

        /// <summary>Queues a refresh of a creature and, when a player is given, of the open appearance editor.</summary>
        void QueueRefreshAndEditor(uint creature, uint player);
    }
}
