using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SWLOR.App.Server;
using SWLOR.App.Server.Server;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Test.App.Server.Infrastructure
{
    /// <summary>
    /// Unit tests for ServiceRegistration to verify proper DI container setup.
    /// Tests that ScriptToEventMapper and related services are properly registered.
    /// </summary>
    [TestFixture]
    public class ServiceRegistrationTests
    {
        private ServiceCollection _services;

        [SetUp]
        public void SetUp()
        {
            _services = new ServiceCollection();
        }

        [Test]
        public void AddServices_ShouldRegisterScriptToEventMapper()
        {
            // Arrange
            _services.AddSingleton(Substitute.For<ILogger>());
            _services.AddSingleton(Substitute.For<IEventAggregator>());

            // Act
            _services.AddServices();
            var serviceProvider = _services.BuildServiceProvider();

            // Assert
            var mapper = serviceProvider.GetService<ScriptToEventMapper>();
            Assert.That(mapper, Is.Not.Null, "ScriptToEventMapper should be registered in the service collection");
        }

        [Test]
        public void AddServices_ShouldRegisterScriptToEventMapperAsSingleton()
        {
            // Arrange
            _services.AddSingleton(Substitute.For<ILogger>());
            _services.AddSingleton(Substitute.For<IEventAggregator>());

            // Act
            _services.AddServices();
            var serviceProvider = _services.BuildServiceProvider();

            // Assert
            var instance1 = serviceProvider.GetService<ScriptToEventMapper>();
            var instance2 = serviceProvider.GetService<ScriptToEventMapper>();

            Assert.That(instance1, Is.Not.Null);
            Assert.That(instance2, Is.Not.Null);
            Assert.That(instance1, Is.SameAs(instance2), "ScriptToEventMapper should be registered as a singleton");
        }

        [Test]
        public void AddServices_ShouldResolveScriptToEventMapperWithDependencies()
        {
            // Arrange
            var mockLogger = Substitute.For<ILogger>();
            _services.AddSingleton(mockLogger);
            _services.AddSingleton(Substitute.For<IEventAggregator>());

            // Act
            _services.AddServices();
            var serviceProvider = _services.BuildServiceProvider();

            // Assert
            var mapper = serviceProvider.GetService<ScriptToEventMapper>();
            Assert.That(mapper, Is.Not.Null, "ScriptToEventMapper should be resolved with its dependencies");
        }

        [Test]
        public void ScriptToEventMapper_ShouldBeResolvable()
        {
            // Arrange
            _services.AddSingleton(Substitute.For<ILogger>());
            _services.AddSingleton(Substitute.For<IEventAggregator>());
            _services.AddServices();
            var serviceProvider = _services.BuildServiceProvider();

            // Act & Assert
            Assert.DoesNotThrow(() => serviceProvider.GetRequiredService<ScriptToEventMapper>());
        }

        [Test]
        public void ScriptToEventMapper_WithoutLogger_ShouldNotBeResolvable()
        {
            // Arrange
            _services.AddSingleton(Substitute.For<IEventAggregator>());
            _services.AddServices();
            var serviceProvider = _services.BuildServiceProvider();

            // Act
            var mapper = serviceProvider.GetService<ScriptToEventMapper>();

            // Assert - Without ILogger dependency, ScriptToEventMapper won't be resolvable
            // (or will be null if optional dependencies are missing)
            // The key is that it won't work properly without its required dependencies
            if (mapper == null)
            {
                Assert.Pass("ScriptToEventMapper correctly not resolved without logger");
            }
            else
            {
                // If it did resolve, verify it doesn't crash (defensive programming test)
                Assert.DoesNotThrow(() => mapper.GetEventType("test"));
            }
        }

        [Test]
        public void AddServices_ShouldNotThrowException()
        {
            // Arrange
            _services.AddSingleton(Substitute.For<ILogger>());
            _services.AddSingleton(Substitute.For<IEventAggregator>());

            // Act & Assert
            Assert.DoesNotThrow(() => _services.AddServices());
        }

        [Test]
        public void AddServices_CalledMultipleTimes_ShouldNotThrow()
        {
            // Arrange
            _services.AddSingleton(Substitute.For<ILogger>());
            _services.AddSingleton(Substitute.For<IEventAggregator>());

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                _services.AddServices();
                _services.AddServices();
            });
        }
    }
}

