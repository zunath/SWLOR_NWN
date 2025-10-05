namespace SWLOR.Shared.Core.Contracts
{
    /// <summary>
    /// Interface for services that require initialization after dependency injection
    /// NWN is single-threaded, so all initialization must be synchronous
    /// </summary>
    public interface IInitializable
    {
        /// <summary>
        /// Initializes the service after all dependencies are resolved
        /// </summary>
        void Initialize();
    }
}
