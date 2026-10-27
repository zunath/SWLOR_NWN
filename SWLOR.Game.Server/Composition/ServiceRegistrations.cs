using Microsoft.Extensions.DependencyInjection;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.Economy;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Domain.World;

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
            // Core
            services.AddSingleton<IDBService, StaticDBAdapter>(); // temporary: the static DB class, until DB is converted

            // Character
            services.AddSingleton<IAchievementService, AchievementService>();

            // Economy
            services.AddSingleton<IBankService, BankService>();

            // Quests
            services.AddSingleton<IFactionService, FactionService>();

            // World
            services.AddSingleton<ITaxiService, TaxiService>();
        }
    }
}
