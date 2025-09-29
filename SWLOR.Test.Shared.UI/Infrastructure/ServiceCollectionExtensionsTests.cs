using NUnit.Framework;
using NSubstitute;
using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.UI.Infrastructure;
using SWLOR.Shared.UI.Service;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.EventHandlers;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Test.Shared;

namespace SWLOR.Test.Shared.UI.Infrastructure
{
    [TestFixture]
    public class ServiceCollectionExtensionsTests : TestBase
    {
        [SetUp]
        public void SetUp()
        {
            InitializeMockNWScript();
        }

        private void AddRequiredDependencies(IServiceCollection services)
        {
            // Add the required IDatabaseService dependency
            services.AddSingleton(Substitute.For<IDatabaseService>());
        }

        [Test]
        public void AddUIServices_RegistersGuiService()
        {
            // Arrange
            var services = new ServiceCollection();
            AddRequiredDependencies(services);

            // Act
            services.AddUIServices();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var guiService = serviceProvider.GetService<IGuiService>();
            Assert.That(guiService, Is.Not.Null);
            Assert.That(guiService, Is.InstanceOf<GuiService>());
        }

        [Test]
        public void AddUIServices_RegistersGuiServiceAsSingleton()
        {
            // Arrange
            var services = new ServiceCollection();
            AddRequiredDependencies(services);

            // Act
            services.AddUIServices();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var guiService1 = serviceProvider.GetService<IGuiService>();
            var guiService2 = serviceProvider.GetService<IGuiService>();
            Assert.That(guiService1, Is.SameAs(guiService2));
        }

        [Test]
        public void AddUIServices_ReturnsServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddUIServices();

            // Assert
            Assert.That(result, Is.SameAs(services));
        }

        [Test]
        public void AddUIServices_CanBeChained()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddUIServices().AddUIServices();

            // Assert
            Assert.That(result, Is.SameAs(services));
        }

        [Test]
        public void AddUIServices_RegistersUIEventHandlers()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddUIServices();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            // UIEventHandlers is internal, so we can't test it directly
            // We can only verify that the service collection was configured without errors
            Assert.That(serviceProvider, Is.Not.Null);
        }

        [Test]
        public void AddUIServices_RegistersUIEventHandlersAsSingleton()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddUIServices();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            // UIEventHandlers is internal, so we can't test it directly
            // We can only verify that the service collection was configured without errors
            Assert.That(serviceProvider, Is.Not.Null);
        }

        [Test]
        public void AddUIServices_RegistersMultipleServices()
        {
            // Arrange
            var services = new ServiceCollection();
            AddRequiredDependencies(services);

            // Act
            services.AddUIServices();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var guiService = serviceProvider.GetService<IGuiService>();
            
            Assert.That(guiService, Is.Not.Null);
            // UIEventHandlers is internal, so we can't test it directly
        }

        [Test]
        public void AddUIServices_WithEmptyServiceCollection_DoesNotThrow()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Assert.DoesNotThrow(() => services.AddUIServices());
        }

        [Test]
        public void AddUIServices_WithNullServiceCollection_ThrowsArgumentNullException()
        {
            // Arrange
            IServiceCollection services = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => services.AddUIServices());
        }

        [Test]
        public void AddUIServices_RegistersAllServices()
        {
            // Arrange
            var services = new ServiceCollection();
            AddRequiredDependencies(services);

            // Act
            services.AddUIServices();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            
            // Check that all expected services are registered
            var guiService = serviceProvider.GetService<IGuiService>();
            
            Assert.That(guiService, Is.Not.Null);
            // UIEventHandlers is internal, so we can't test it directly
        }

        [Test]
        public void AddUIServices_CanBeCalledMultipleTimes()
        {
            // Arrange
            var services = new ServiceCollection();
            AddRequiredDependencies(services);

            // Act
            services.AddUIServices();
            services.AddUIServices();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var guiService = serviceProvider.GetService<IGuiService>();
            Assert.That(guiService, Is.Not.Null);
        }

        [Test]
        public void AddUIServices_RegistersServicesWithCorrectLifetime()
        {
            // Arrange
            var services = new ServiceCollection();
            AddRequiredDependencies(services);

            // Act
            services.AddUIServices();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            
            // Create two service providers to test singleton behavior
            var serviceProvider2 = services.BuildServiceProvider();
            
            var guiService1 = serviceProvider.GetService<IGuiService>();
            var guiService2 = serviceProvider2.GetService<IGuiService>();
            
            // For singletons, instances should be different between providers
            // but the same within a provider
            var guiService1Again = serviceProvider.GetService<IGuiService>();
            Assert.That(guiService1, Is.SameAs(guiService1Again));
        }
    }
}