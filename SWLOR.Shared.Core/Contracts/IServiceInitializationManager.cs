using SWLOR.Shared.Core.Infrastructure;

namespace SWLOR.Shared.Core.Contracts;

public interface IServiceInitializationManager
{
    /// <summary>
    /// Initializes all services that implement IServiceInitializer
    /// </summary>
    void InitializeAllServices();

    /// <summary>
    /// Gets the initialization results for monitoring
    /// </summary>
    IReadOnlyList<ServiceInitializationManager.InitializationResult> GetInitializationResults();
}