using Microsoft.Extensions.DependencyInjection;

namespace SWLOR.Game.Server.Composition
{
    /// <summary>
    /// Every service the container knows, one line each, grouped by domain (the P0-8 domain map). Converting a service
    /// adds its line here and its entry to <c>SWLOR.Architecture.Tests/ConvertedServices.json</c>.
    /// Every game service is a singleton: its state lives for the life of the process.
    /// </summary>
    internal static class ServiceRegistrations
    {
        internal static void Register(IServiceCollection services)
        {
        }
    }
}
