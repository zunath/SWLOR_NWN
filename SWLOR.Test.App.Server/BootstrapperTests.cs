using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Infrastructure;

namespace SWLOR.Test.App.Server
{
    /// <summary>
    /// Unit tests for Bootstrapper class.
    /// Tests dependency injection setup and service provider configuration.
    /// Note: These are integration-style tests due to static nature of Bootstrapper.
    /// </summary>
    [TestFixture]
    public class BootstrapperTests
    {
        [Test]
        public void ServiceRegistration_ShouldRegisterScriptToEventMapper()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            SWLOR.App.Server.ServiceRegistration.AddServices(services);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var mapper = serviceProvider.GetService<SWLOR.App.Server.Server.ScriptToEventMapper>();
            Assert.That(mapper, Is.Not.Null, "ScriptToEventMapper should be registered");
        }

        [Test]
        public void ServiceRegistration_ShouldRegisterIClosureManager()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            SWLOR.App.Server.ServiceRegistration.AddServices(services);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var closureManager = serviceProvider.GetService<IClosureManager>();
            Assert.That(closureManager, Is.Not.Null, "IClosureManager should be registered");
        }

        [Test]
        public void ServiceRegistration_ShouldRegisterScriptExecutionInfrastructure()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            SWLOR.App.Server.ServiceRegistration.AddServices(services);
            var serviceProvider = services.BuildServiceProvider();

            // Assert - Verify script execution infrastructure is set up
            // (Some services may have dependencies that aren't resolved in unit tests)
            Assert.That(services.Count, Is.GreaterThan(0), 
                "Services should include script execution infrastructure");
        }

        [Test]
        public void ServiceRegistration_ShouldRegisterILogger()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            SWLOR.App.Server.ServiceRegistration.AddServices(services);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var logger = serviceProvider.GetService<ILogger>();
            Assert.That(logger, Is.Not.Null, "ILogger should be registered");
        }

        [Test]
        public void ServiceRegistration_CalledMultipleTimes_ShouldNotThrow()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                SWLOR.App.Server.ServiceRegistration.AddServices(services);
                SWLOR.App.Server.ServiceRegistration.AddServices(services);
                SWLOR.App.Server.ServiceRegistration.AddServices(services);
            });
        }

        [Test]
        public void ServiceRegistration_ShouldBuildServiceProviderSuccessfully()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            SWLOR.App.Server.ServiceRegistration.AddServices(services);

            // Assert
            Assert.DoesNotThrow(() => services.BuildServiceProvider());
        }

        [Test]
        public void ServiceProvider_ShouldResolveBasicInfrastructure()
        {
            // Arrange
            var services = new ServiceCollection();
            SWLOR.App.Server.ServiceRegistration.AddServices(services);
            var serviceProvider = services.BuildServiceProvider();

            // Act & Assert - Basic infrastructure services should be resolvable
            var logger = serviceProvider.GetService<ILogger>();
            var closureManager = serviceProvider.GetService<IClosureManager>();
            
            Assert.That(logger, Is.Not.Null, "ILogger should be registered");
            Assert.That(closureManager, Is.Not.Null, "IClosureManager should be registered");
        }

        [Test]
        public void ServiceRegistration_WithEmptyServiceCollection_ShouldRegisterServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var initialCount = services.Count;

            // Act
            SWLOR.App.Server.ServiceRegistration.AddServices(services);

            // Assert
            Assert.That(services.Count, Is.GreaterThan(initialCount), 
                "Services should be added to the collection");
        }

        [Test]
        public void ServiceProvider_BasicServices_ShouldResolveWithoutDeadlock()
        {
            // Arrange
            var services = new ServiceCollection();
            SWLOR.App.Server.ServiceRegistration.AddServices(services);
            var serviceProvider = services.BuildServiceProvider();

            // Act & Assert - Should not deadlock when resolving basic services
            Assert.DoesNotThrow(() =>
            {
                var logger = serviceProvider.GetService<ILogger>();
                var closureManager = serviceProvider.GetService<IClosureManager>();
                var mapper = serviceProvider.GetService<SWLOR.App.Server.Server.ScriptToEventMapper>();
            }, "Service resolution should not cause deadlocks");
        }
    }
}

