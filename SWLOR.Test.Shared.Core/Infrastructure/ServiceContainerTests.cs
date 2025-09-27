using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Core.Infrastructure;

namespace SWLOR.Test.Shared.Core.Infrastructure
{
    [TestFixture]
    public class ServiceContainerTests
    {
        [TearDown]
        public void TearDown()
        {
            // Reset the service container after each test
            var field = typeof(ServiceContainer).GetField("_serviceProvider", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            field?.SetValue(null, null);
        }

        [Test]
        public void IsInitialized_WhenNotInitialized_ShouldReturnFalse()
        {
            // Act
            var result = ServiceContainer.IsInitialized;

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsInitialized_WhenInitialized_ShouldReturnTrue()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            ServiceContainer.Initialize(serviceProvider);

            // Act
            var result = ServiceContainer.IsInitialized;

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void Initialize_WithNullServiceProvider_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => ServiceContainer.Initialize(null));
        }

        [Test]
        public void Initialize_WithValidServiceProvider_ShouldSetServiceProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();

            // Act
            ServiceContainer.Initialize(serviceProvider);

            // Assert
            Assert.That(ServiceContainer.IsInitialized, Is.True);
        }

        [Test]
        public void Initialize_WithValidServiceProvider_ShouldAllowMultipleCalls()
        {
            // Arrange
            var services1 = new ServiceCollection();
            var serviceProvider1 = services1.BuildServiceProvider();
            
            var services2 = new ServiceCollection();
            var serviceProvider2 = services2.BuildServiceProvider();

            // Act
            ServiceContainer.Initialize(serviceProvider1);
            ServiceContainer.Initialize(serviceProvider2);

            // Assert
            Assert.That(ServiceContainer.IsInitialized, Is.True);
        }

        [Test]
        public void GetService_WhenNotInitialized_ShouldThrowInvalidOperationException()
        {
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => ServiceContainer.GetService<object>());
            Assert.That(exception.Message, Does.Contain("Service container has not been initialized"));
        }

        [Test]
        public void GetService_WithRegisteredService_ShouldReturnService()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ITestService, TestService>();
            var serviceProvider = services.BuildServiceProvider();
            ServiceContainer.Initialize(serviceProvider);

            // Act
            var result = ServiceContainer.GetService<ITestService>();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<TestService>());
        }

        [Test]
        public void GetService_WithUnregisteredService_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            ServiceContainer.Initialize(serviceProvider);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => ServiceContainer.GetService<ITestService>());
        }

        [Test]
        public void GetServiceOptional_WhenNotInitialized_ShouldThrowInvalidOperationException()
        {
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => ServiceContainer.GetServiceOptional<object>());
            Assert.That(exception.Message, Does.Contain("Service container has not been initialized"));
        }

        [Test]
        public void GetServiceOptional_WithRegisteredService_ShouldReturnService()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ITestService, TestService>();
            var serviceProvider = services.BuildServiceProvider();
            ServiceContainer.Initialize(serviceProvider);

            // Act
            var result = ServiceContainer.GetServiceOptional<ITestService>();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<TestService>());
        }

        [Test]
        public void GetServiceOptional_WithUnregisteredService_ShouldReturnNull()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            ServiceContainer.Initialize(serviceProvider);

            // Act
            var result = ServiceContainer.GetServiceOptional<ITestService>();

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetService_WithTransientService_ShouldReturnNewInstanceEachTime()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddTransient<ITestService, TestService>();
            var serviceProvider = services.BuildServiceProvider();
            ServiceContainer.Initialize(serviceProvider);

            // Act
            var result1 = ServiceContainer.GetService<ITestService>();
            var result2 = ServiceContainer.GetService<ITestService>();

            // Assert
            Assert.That(result1, Is.Not.Null);
            Assert.That(result2, Is.Not.Null);
            Assert.That(result1, Is.Not.SameAs(result2));
        }

        [Test]
        public void GetService_WithScopedService_ShouldReturnSameInstanceInSameScope()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddScoped<ITestService, TestService>();
            var serviceProvider = services.BuildServiceProvider();
            ServiceContainer.Initialize(serviceProvider);

            // Act
            var result1 = ServiceContainer.GetService<ITestService>();
            var result2 = ServiceContainer.GetService<ITestService>();

            // Assert
            Assert.That(result1, Is.Not.Null);
            Assert.That(result2, Is.Not.Null);
            Assert.That(result1, Is.SameAs(result2));
        }

        [Test]
        public void GetService_WithSingletonService_ShouldReturnSameInstance()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ITestService, TestService>();
            var serviceProvider = services.BuildServiceProvider();
            ServiceContainer.Initialize(serviceProvider);

            // Act
            var result1 = ServiceContainer.GetService<ITestService>();
            var result2 = ServiceContainer.GetService<ITestService>();

            // Assert
            Assert.That(result1, Is.Not.Null);
            Assert.That(result2, Is.Not.Null);
            Assert.That(result1, Is.SameAs(result2));
        }

        [Test]
        public void GetService_WithGenericService_ShouldReturnCorrectService()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ITestGenericService<string>, TestGenericService<string>>();
            var serviceProvider = services.BuildServiceProvider();
            ServiceContainer.Initialize(serviceProvider);

            // Act
            var result = ServiceContainer.GetService<ITestGenericService<string>>();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<TestGenericService<string>>());
        }

        [Test]
        public void GetService_WithNullValue_ShouldReturnNull()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<string>((sp) => null);
            var serviceProvider = services.BuildServiceProvider();
            ServiceContainer.Initialize(serviceProvider);

            // Act
            var result = ServiceContainer.GetServiceOptional<string>();

            // Assert
            Assert.That(result, Is.Null);
        }

        public interface ITestService
        {
            string GetValue();
        }

        public class TestService : ITestService
        {
            public string GetValue() => "Test";
        }

        public interface ITestGenericService<T>
        {
            T GetValue();
        }

        public class TestGenericService<T> : ITestGenericService<T>
        {
            public T GetValue() => default(T);
        }
    }
}
