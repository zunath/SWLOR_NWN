using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Events.Infrastructure;

namespace SWLOR.Test.Shared.Events.Infrastructure
{
    [TestFixture]
    public class ServiceCollectionExtensionsTests
    {
        private IServiceCollection _services;

        [SetUp]
        public void SetUp()
        {
            _services = new ServiceCollection();
        }

        [Test]
        public void AddEventingServices_WithValidServices_ReturnsSameServiceCollection()
        {
            // Act
            var result = _services.AddEventingServices();

            // Assert
            Assert.That(result, Is.EqualTo(_services));
        }

        [Test]
        public void AddEventingServices_CanBeCalledMultipleTimes()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => 
            {
                _services.AddEventingServices();
                _services.AddEventingServices();
            });
        }

        [Test]
        public void AddEventingServices_WithValidServices_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _services.AddEventingServices());
        }
    }
}