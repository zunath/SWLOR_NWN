using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SWLOR.Component.Character.Service;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Test.Component.Character.Service
{
    [TestFixture]
    public class CurrencyServiceTests
    {
        private IDatabaseService _mockDatabaseService;
        private IServiceProvider _mockServiceProvider;
        private IGuiService _mockGuiService;
        private CurrencyService _currencyService;

        [SetUp]
        public void SetUp()
        {
            _mockDatabaseService = Substitute.For<IDatabaseService>();
            _mockGuiService = Substitute.For<IGuiService>();
            
            // Create a real service provider with the mock services
            var services = new ServiceCollection();
            services.AddSingleton(_mockGuiService);
            _mockServiceProvider = services.BuildServiceProvider();

            _currencyService = new CurrencyService(_mockDatabaseService, _mockServiceProvider);
        }

        [TearDown]
        public void TearDown()
        {
            // Dispose of the service provider if it implements IDisposable
            if (_mockServiceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }


        // Note: CurrencyService tests removed because they require NWN API calls (GetObjectUUID)
        // These would be better suited for integration tests
    }
}
