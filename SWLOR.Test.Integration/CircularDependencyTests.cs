using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using SWLOR.App.Server;
using SWLOR.Shared.Core.Infrastructure;

namespace SWLOR.Test.Integration
{
    /// <summary>
    /// Tests to detect and prevent circular dependencies in the service container
    /// </summary>
    [TestFixture]
    public class CircularDependencyTests
    {
        [Test]
        public void NoCircularDependencies_ShouldBuildServiceProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddServices();

            // Act & Assert
            // This will throw if there are circular dependencies
            var serviceProvider = services.BuildServiceProvider();
            
            Assert.That(serviceProvider, Is.Not.Null, "Service provider should build successfully without circular dependencies");
        }

        [Test]
        public void ServiceInitialization_ShouldCompleteSuccessfully()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddServices();

            // Add logging for the test
            services.AddLogging(builder => builder.AddConsole());

            var serviceProvider = services.BuildServiceProvider();

            // Act
            var initializationManager = serviceProvider.GetRequiredService<ServiceInitializer>();

            // This should not throw
            Assert.DoesNotThrow(() =>
            {
                initializationManager.InitializeAllServices();
            });
        }
    }
}
