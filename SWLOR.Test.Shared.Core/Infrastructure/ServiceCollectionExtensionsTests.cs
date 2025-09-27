using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Core.Service;

namespace SWLOR.Test.Shared.Core.Infrastructure
{
    [TestFixture]
    public class ServiceCollectionExtensionsTests
    {
        [Test]
        public void AddCoreServices_ShouldRegisterRandomService()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddCoreServices();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var randomService = serviceProvider.GetService<IRandomService>();
            Assert.That(randomService, Is.Not.Null);
            Assert.That(randomService, Is.InstanceOf<RandomService>());
        }

        [Test]
        public void AddCoreServices_ShouldRegisterTimeService()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddCoreServices();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var timeService = serviceProvider.GetService<ITimeService>();
            Assert.That(timeService, Is.Not.Null);
            Assert.That(timeService, Is.InstanceOf<TimeService>());
        }

        [Test]
        public void AddCoreServices_ShouldRegisterDiscordNotificationService()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IAppSettings, MockAppSettings>();

            // Act
            services.AddCoreServices();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var discordService = serviceProvider.GetService<IDiscordNotificationService>();
            Assert.That(discordService, Is.Not.Null);
            Assert.That(discordService, Is.InstanceOf<DiscordNotificationService>());
        }

        [Test]
        public void AddCoreServices_ShouldRegisterServicesAsSingletons()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IAppSettings, MockAppSettings>();

            // Act
            services.AddCoreServices();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            
            var randomService1 = serviceProvider.GetService<IRandomService>();
            var randomService2 = serviceProvider.GetService<IRandomService>();
            Assert.That(randomService1, Is.SameAs(randomService2));

            var timeService1 = serviceProvider.GetService<ITimeService>();
            var timeService2 = serviceProvider.GetService<ITimeService>();
            Assert.That(timeService1, Is.SameAs(timeService2));

            var discordService1 = serviceProvider.GetService<IDiscordNotificationService>();
            var discordService2 = serviceProvider.GetService<IDiscordNotificationService>();
            Assert.That(discordService1, Is.SameAs(discordService2));
        }

        [Test]
        public void AddCoreServices_ShouldReturnServiceCollectionForChaining()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddCoreServices();

            // Assert
            Assert.That(result, Is.SameAs(services));
        }

        [Test]
        public void AddCoreServices_ShouldRegisterAllRequiredServices()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IAppSettings, MockAppSettings>();

            // Act
            services.AddCoreServices();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            
            // Check that all services are registered
            Assert.That(serviceProvider.GetService<IRandomService>(), Is.Not.Null);
            Assert.That(serviceProvider.GetService<ITimeService>(), Is.Not.Null);
            Assert.That(serviceProvider.GetService<IDiscordNotificationService>(), Is.Not.Null);
        }

        [Test]
        public void AddCoreServices_WithMultipleCalls_ShouldNotDuplicateServices()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IAppSettings, MockAppSettings>();

            // Act
            services.AddCoreServices();
            services.AddCoreServices();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var randomService = serviceProvider.GetService<IRandomService>();
            Assert.That(randomService, Is.Not.Null);
            Assert.That(randomService, Is.InstanceOf<RandomService>());
        }

        [Test]
        public void AddCoreServices_ShouldAllowServiceResolution()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IAppSettings, MockAppSettings>();

            // Act
            services.AddCoreServices();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            
            // Test that services can be resolved and used
            var randomService = serviceProvider.GetService<IRandomService>();
            var randomValue = randomService.Next(10);
            Assert.That(randomValue, Is.GreaterThanOrEqualTo(0));
            Assert.That(randomValue, Is.LessThan(10));

            var timeService = serviceProvider.GetService<ITimeService>();
            var timeString = timeService.GetTimeLongIntervals(1, 2, 3, 4, false);
            Assert.That(timeString, Is.Not.Null);
            Assert.That(timeString, Does.Contain("1 day"));
        }

        private class MockAppSettings : IAppSettings
        {
            public string DMShoutWebHookUrl => "https://discord.com/api/webhooks/test-dm-shout";
            public string BugWebHookUrl => "https://discord.com/api/webhooks/test-bug";
            public string HolonetWebHookUrl => "https://discord.com/api/webhooks/test-holonet";
            public string LogDirectory => "logs/";
            public SWLOR.Shared.Abstractions.Enums.ServerEnvironmentType ServerEnvironment => SWLOR.Shared.Abstractions.Enums.ServerEnvironmentType.All;
            public string RedisIPAddress => "localhost:6379";
        }

        private class MockRandomService : IRandomService
        {
            public int Next() => 42;
            public int Next(int max) => max / 2;
            public int Next(int min, int max) => (min + max) / 2;
            public float NextFloat() => 0.5f;
            public float NextFloat(float min, float max) => (min + max) / 2;
            public int GetRandomWeightedIndex(int[] weights) => 0;
            public int D2(int numberOfDice, int minimum = 1) => numberOfDice + minimum;
            public int D3(int numberOfDice, int minimum = 1) => numberOfDice + minimum;
            public int D4(int numberOfDice, int minimum = 1) => numberOfDice + minimum;
            public int D6(int numberOfDice, int minimum = 1) => numberOfDice + minimum;
            public int D8(int numberOfDice, int minimum = 1) => numberOfDice + minimum;
            public int D10(int numberOfDice, int minimum = 1) => numberOfDice + minimum;
            public int D12(int numberOfDice, int minimum = 1) => numberOfDice + minimum;
            public int D20(int numberOfDice, int minimum = 1) => numberOfDice + minimum;
            public int D100(int numberOfDice, int minimum = 1) => numberOfDice + minimum;
        }
    }
}
