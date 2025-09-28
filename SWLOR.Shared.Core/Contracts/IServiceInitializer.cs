namespace SWLOR.Shared.Core.Contracts
{
    /// <summary>
    /// Interface for services that require initialization after dependency injection
    /// NWN is single-threaded, so all initialization must be synchronous
    /// </summary>
    public interface IServiceInitializer
    {
        /// <summary>
        /// Initializes the service after all dependencies are resolved
        /// </summary>
        void Initialize();

        /// <summary>
        /// Gets the initialization priority (lower numbers initialize first)
        /// </summary>
        int InitializationPriority { get; }

        /// <summary>
        /// Gets the name of the service for logging purposes
        /// </summary>
        string ServiceName { get; }
    }
}
