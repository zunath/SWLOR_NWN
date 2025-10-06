namespace SWLOR.Shared.Events.Contracts
{
    /// <summary>
    /// Service responsible for discovering and registering event handlers decorated with ScriptHandler attributes.
    /// </summary>
    public interface IEventHandlerDiscoveryService : IDisposable
    {
        /// <summary>
        /// Discovers and registers all event handlers decorated with ScriptHandler attributes.
        /// </summary>
        void DiscoverAndRegisterHandlers();
    }
}
