using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Core.Log.LogGroup;

namespace SWLOR.Test.Shared.Core.Infrastructure
{
    [TestFixture]
    public class ServiceInitializerTests
    {
        private ILogger _mockLogger;
        private ServiceInitializer _serviceInitializer;

        [SetUp]
        public void SetUp()
        {
            _mockLogger = Substitute.For<ILogger>();
        }

        [Test]
        public void Constructor_WithValidParameters_ShouldCreateInstance()
        {
            // Arrange
            var mockServiceProvider = Substitute.For<IServiceProvider>();

            // Act
            var initializer = new ServiceInitializer(mockServiceProvider, _mockLogger);

            // Assert
            Assert.That(initializer, Is.Not.Null);
        }

        [Test]
        public void InitializeAllServices_WithNoServices_ShouldLogAndCompleteSuccessfully()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            _serviceInitializer = new ServiceInitializer(serviceProvider, _mockLogger);

            // Act
            _serviceInitializer.InitializeAllServices();

            // Assert
            _mockLogger.Received(1).Write<InfrastructureLogGroup>("Starting service initialization...");
            _mockLogger.Received(1).Write<InfrastructureLogGroup>("Found 0 services requiring initialization");
        }

        [Test]
        public void InitializeAllServices_WithSingleService_ShouldInitializeSuccessfully()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockService = Substitute.For<IInitializable>();
            services.AddSingleton(mockService);
            var serviceProvider = services.BuildServiceProvider();
            _serviceInitializer = new ServiceInitializer(serviceProvider, _mockLogger);

            // Act
            _serviceInitializer.InitializeAllServices();

            // Assert
            _mockLogger.Received(1).Write<InfrastructureLogGroup>("Starting service initialization...");
            _mockLogger.Received(1).Write<InfrastructureLogGroup>("Found 1 services requiring initialization");
            _mockLogger.Received(1).Write<InfrastructureLogGroup>($"Initializing {mockService.GetType()}");
            _mockLogger.Received(1).Write<InfrastructureLogGroup>($"Successfully initialized {mockService.GetType()}");
            mockService.Received(1).Initialize();
        }

        [Test]
        public void InitializeAllServices_WithMultipleServices_ShouldInitializeAllSuccessfully()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockService1 = Substitute.For<IInitializable>();
            var mockService2 = Substitute.For<IInitializable>();
            var mockService3 = Substitute.For<IInitializable>();
            services.AddSingleton(mockService1);
            services.AddSingleton(mockService2);
            services.AddSingleton(mockService3);
            var serviceProvider = services.BuildServiceProvider();
            _serviceInitializer = new ServiceInitializer(serviceProvider, _mockLogger);

            // Act
            _serviceInitializer.InitializeAllServices();

            // Assert
            _mockLogger.Received(1).Write<InfrastructureLogGroup>("Starting service initialization...");
            _mockLogger.Received(1).Write<InfrastructureLogGroup>("Found 3 services requiring initialization");
            
            mockService1.Received(1).Initialize();
            mockService2.Received(1).Initialize();
            mockService3.Received(1).Initialize();
            
            _mockLogger.Received(3).Write<InfrastructureLogGroup>(Arg.Is<string>(s => s.StartsWith("Initializing")));
            _mockLogger.Received(3).Write<InfrastructureLogGroup>(Arg.Is<string>(s => s.StartsWith("Successfully initialized")));
        }

        [Test]
        public void InitializeAllServices_WithServiceThrowingException_ShouldLogErrorAndContinue()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockService1 = Substitute.For<IInitializable>();
            var mockService2 = Substitute.For<IInitializable>();
            var mockService3 = Substitute.For<IInitializable>();
            
            services.AddSingleton(mockService1);
            services.AddSingleton(mockService2);
            services.AddSingleton(mockService3);
            
            var serviceProvider = services.BuildServiceProvider();
            _serviceInitializer = new ServiceInitializer(serviceProvider, _mockLogger);

            var exception = new InvalidOperationException("Test exception");
            mockService2.When(x => x.Initialize()).Throw(exception);

            // Act
            _serviceInitializer.InitializeAllServices();

            // Assert
            _mockLogger.Received(1).Write<InfrastructureLogGroup>("Starting service initialization...");
            _mockLogger.Received(1).Write<InfrastructureLogGroup>("Found 3 services requiring initialization");
            
            // Service 1 should be initialized successfully
            mockService1.Received(1).Initialize();
            
            // Service 2 should throw exception and be logged
            mockService2.Received(1).Initialize();
            
            // Service 3 should still be initialized despite service 2 failing
            mockService3.Received(1).Initialize();
            
            // Verify logging calls
            _mockLogger.Received(3).Write<InfrastructureLogGroup>(Arg.Is<string>(s => s.StartsWith("Initializing")));
            _mockLogger.Received(2).Write<InfrastructureLogGroup>(Arg.Is<string>(s => s.StartsWith("Successfully initialized")));
            _mockLogger.Received(1).Write<InfrastructureLogGroup>(Arg.Is<string>(s => s.StartsWith("Failed to initialize")));
        }

        [Test]
        public void InitializeAllServices_WithAllServicesThrowingExceptions_ShouldLogAllErrors()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockService1 = Substitute.For<IInitializable>();
            var mockService2 = Substitute.For<IInitializable>();
            
            services.AddSingleton(mockService1);
            services.AddSingleton(mockService2);
            
            var serviceProvider = services.BuildServiceProvider();
            _serviceInitializer = new ServiceInitializer(serviceProvider, _mockLogger);

            var exception1 = new InvalidOperationException("Test exception 1");
            var exception2 = new ArgumentException("Test exception 2");
            mockService1.When(x => x.Initialize()).Throw(exception1);
            mockService2.When(x => x.Initialize()).Throw(exception2);

            // Act
            _serviceInitializer.InitializeAllServices();

            // Assert
            _mockLogger.Received(1).Write<InfrastructureLogGroup>("Starting service initialization...");
            _mockLogger.Received(1).Write<InfrastructureLogGroup>("Found 2 services requiring initialization");
            
            // Both services should be attempted and both failures should be logged
            mockService1.Received(1).Initialize();
            mockService2.Received(1).Initialize();
            
            // Verify logging calls
            _mockLogger.Received(2).Write<InfrastructureLogGroup>(Arg.Is<string>(s => s.StartsWith("Initializing")));
            _mockLogger.Received(2).Write<InfrastructureLogGroup>(Arg.Is<string>(s => s.StartsWith("Failed to initialize")));
        }

        [Test]
        public void InitializeAllServices_ShouldInitializeServicesInOrder()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockService1 = Substitute.For<IInitializable>();
            var mockService2 = Substitute.For<IInitializable>();
            var mockService3 = Substitute.For<IInitializable>();
            
            services.AddSingleton(mockService1);
            services.AddSingleton(mockService2);
            services.AddSingleton(mockService3);
            
            var serviceProvider = services.BuildServiceProvider();
            _serviceInitializer = new ServiceInitializer(serviceProvider, _mockLogger);

            var callOrder = new List<IInitializable>();
            mockService1.When(x => x.Initialize()).Do(_ => callOrder.Add(mockService1));
            mockService2.When(x => x.Initialize()).Do(_ => callOrder.Add(mockService2));
            mockService3.When(x => x.Initialize()).Do(_ => callOrder.Add(mockService3));

            // Act
            _serviceInitializer.InitializeAllServices();

            // Assert
            Assert.That(callOrder.Count, Is.EqualTo(3));
            Assert.That(callOrder[0], Is.SameAs(mockService1));
            Assert.That(callOrder[1], Is.SameAs(mockService2));
            Assert.That(callOrder[2], Is.SameAs(mockService3));
        }

        [Test]
        public void InitializeAllServices_ShouldLogInfrastructureLogGroup()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockService = Substitute.For<IInitializable>();
            services.AddSingleton(mockService);
            var serviceProvider = services.BuildServiceProvider();
            _serviceInitializer = new ServiceInitializer(serviceProvider, _mockLogger);

            // Act
            _serviceInitializer.InitializeAllServices();

            // Assert
            _mockLogger.Received().Write<InfrastructureLogGroup>(Arg.Any<string>());
        }
    }
}