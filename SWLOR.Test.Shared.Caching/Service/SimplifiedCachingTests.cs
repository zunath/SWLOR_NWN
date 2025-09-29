using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Service;
using SWLOR.Shared.Caching.Extensions;
using SWLOR.Shared.Caching.EventHandlers;

namespace SWLOR.Test.Shared.Caching.Service
{
    [TestFixture]
    public class SimplifiedCachingTests : TestBase
    {
        [Test]
        public void GenericCacheService_Constructor_WithNullProvider_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => new GenericCacheService(null));
        }

        [Test]
        public void GenericCacheService_Constructor_WithProvider_ShouldNotThrow()
        {
            // Arrange
            var mockProvider = Substitute.For<IServiceProvider>();

            // Act & Assert
            Assert.DoesNotThrow(() => new GenericCacheService(mockProvider));
        }

        [Test]
        public void GenericCacheService_BuildEnumCache_ShouldReturnBuilder()
        {
            // Arrange
            var service = new GenericCacheService(null);

            // Act
            var result = service.BuildEnumCache<TestEnum, TestAttribute>();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void GenericCacheService_BuildInterfaceCache_ShouldReturnBuilder()
        {
            // Arrange
            var service = new GenericCacheService(null);

            // Act
            var result = service.BuildInterfaceCache<ITestInterface, string, string>();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void EnumCache_Constructor_ShouldInitializeProperties()
        {
            // Arrange
            var allItems = new Dictionary<TestEnum, TestAttribute>();
            var filteredCaches = new Dictionary<string, Dictionary<TestEnum, TestAttribute>>();
            var groupedCaches = new Dictionary<string, object>();
            var filteredGroupedCaches = new Dictionary<string, object>();

            // Act
            var cache = new EnumCache<TestEnum, TestAttribute>(allItems, filteredCaches, groupedCaches, filteredGroupedCaches);

            // Assert
            Assert.That(cache.AllItems, Is.EqualTo(allItems));
        }

        [Test]
        public void EnumCache_GetFilteredCache_WithExistingName_ShouldReturnCache()
        {
            // Arrange
            var allItems = new Dictionary<TestEnum, TestAttribute>();
            var filteredCaches = new Dictionary<string, Dictionary<TestEnum, TestAttribute>>
            {
                { "test", new Dictionary<TestEnum, TestAttribute>() }
            };
            var groupedCaches = new Dictionary<string, object>();
            var filteredGroupedCaches = new Dictionary<string, object>();

            var cache = new EnumCache<TestEnum, TestAttribute>(allItems, filteredCaches, groupedCaches, filteredGroupedCaches);

            // Act
            var result = cache.GetFilteredCache("test");

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void EnumCache_GetFilteredCache_WithNonExistingName_ShouldReturnNull()
        {
            // Arrange
            var allItems = new Dictionary<TestEnum, TestAttribute>();
            var filteredCaches = new Dictionary<string, Dictionary<TestEnum, TestAttribute>>();
            var groupedCaches = new Dictionary<string, object>();
            var filteredGroupedCaches = new Dictionary<string, object>();

            var cache = new EnumCache<TestEnum, TestAttribute>(allItems, filteredCaches, groupedCaches, filteredGroupedCaches);

            // Act
            var result = cache.GetFilteredCache("nonexistent");

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void InterfaceCache_Constructor_ShouldInitializeProperties()
        {
            // Arrange
            var allItems = new Dictionary<string, string>();
            var filteredCaches = new Dictionary<string, Dictionary<string, string>>();
            var groupedCaches = new Dictionary<string, object>();
            var filteredGroupedCaches = new Dictionary<string, object>();

            // Act
            var cache = new InterfaceCache<string, string>(allItems, filteredCaches, groupedCaches, filteredGroupedCaches);

            // Assert
            Assert.That(cache.AllItems, Is.EqualTo(allItems));
        }

        [Test]
        public void InterfaceCache_GetFilteredCache_WithExistingName_ShouldReturnCache()
        {
            // Arrange
            var allItems = new Dictionary<string, string>();
            var filteredCaches = new Dictionary<string, Dictionary<string, string>>
            {
                { "test", new Dictionary<string, string>() }
            };
            var groupedCaches = new Dictionary<string, object>();
            var filteredGroupedCaches = new Dictionary<string, object>();

            var cache = new InterfaceCache<string, string>(allItems, filteredCaches, groupedCaches, filteredGroupedCaches);

            // Act
            var result = cache.GetFilteredCache("test");

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void InterfaceCache_GetFilteredCache_WithNonExistingName_ShouldReturnNull()
        {
            // Arrange
            var allItems = new Dictionary<string, string>();
            var filteredCaches = new Dictionary<string, Dictionary<string, string>>();
            var groupedCaches = new Dictionary<string, object>();
            var filteredGroupedCaches = new Dictionary<string, object>();

            var cache = new InterfaceCache<string, string>(allItems, filteredCaches, groupedCaches, filteredGroupedCaches);

            // Act
            var result = cache.GetFilteredCache("nonexistent");

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void ServiceCollectionExtensions_AddCacheServices_ShouldRegisterServices()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddCacheServices();

            // Assert
            Assert.That(result, Is.EqualTo(services));
            
            var serviceProvider = services.BuildServiceProvider();
            Assert.DoesNotThrow(() => serviceProvider.GetRequiredService<IGenericCacheService>());
        }

        [Test]
        public void EnumCache_ShouldStoreAndRetrieveData()
        {
            // Arrange
            var allItems = new Dictionary<TestEnum, TestAttribute>
            {
                { TestEnum.Value1, new TestAttribute { Name = "Test1" } },
                { TestEnum.Value2, new TestAttribute { Name = "Test2" } }
            };
            var filteredCaches = new Dictionary<string, Dictionary<TestEnum, TestAttribute>>();
            var groupedCaches = new Dictionary<string, object>();
            var filteredGroupedCaches = new Dictionary<string, object>();

            // Act
            var cache = new EnumCache<TestEnum, TestAttribute>(allItems, filteredCaches, groupedCaches, filteredGroupedCaches);

            // Assert
            Assert.That(cache.AllItems, Is.EqualTo(allItems));
            Assert.That(cache.AllItems.Count, Is.EqualTo(2));
        }

        [Test]
        public void EnumCache_GetFilteredCache_ShouldReturnFilteredData()
        {
            // Arrange
            var allItems = new Dictionary<TestEnum, TestAttribute>
            {
                { TestEnum.Value1, new TestAttribute { Name = "Test1" } },
                { TestEnum.Value2, new TestAttribute { Name = "Test2" } }
            };
            var filteredCaches = new Dictionary<string, Dictionary<TestEnum, TestAttribute>>
            {
                { "filter1", new Dictionary<TestEnum, TestAttribute> { { TestEnum.Value1, new TestAttribute { Name = "Test1" } } } }
            };
            var groupedCaches = new Dictionary<string, object>();
            var filteredGroupedCaches = new Dictionary<string, object>();

            var cache = new EnumCache<TestEnum, TestAttribute>(allItems, filteredCaches, groupedCaches, filteredGroupedCaches);

            // Act
            var result = cache.GetFilteredCache("filter1");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.ContainsKey(TestEnum.Value1), Is.True);
        }

        [Test]
        public void InterfaceCache_ShouldStoreAndRetrieveData()
        {
            // Arrange
            var allItems = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };
            var filteredCaches = new Dictionary<string, Dictionary<string, string>>();
            var groupedCaches = new Dictionary<string, object>();
            var filteredGroupedCaches = new Dictionary<string, object>();

            // Act
            var cache = new InterfaceCache<string, string>(allItems, filteredCaches, groupedCaches, filteredGroupedCaches);

            // Assert
            Assert.That(cache.AllItems, Is.EqualTo(allItems));
            Assert.That(cache.AllItems.Count, Is.EqualTo(2));
        }

        [Test]
        public void InterfaceCache_GetFilteredCache_ShouldReturnFilteredData()
        {
            // Arrange
            var allItems = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };
            var filteredCaches = new Dictionary<string, Dictionary<string, string>>
            {
                { "filter1", new Dictionary<string, string> { { "key1", "value1" } } }
            };
            var groupedCaches = new Dictionary<string, object>();
            var filteredGroupedCaches = new Dictionary<string, object>();

            var cache = new InterfaceCache<string, string>(allItems, filteredCaches, groupedCaches, filteredGroupedCaches);

            // Act
            var result = cache.GetFilteredCache("filter1");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.ContainsKey("key1"), Is.True);
        }

        [Test]
        public void EnumCacheBuilder_WithAllItems_ShouldBuildCache()
        {
            // Arrange
            var builder = new EnumCacheBuilder<TestEnum, TestAttribute>();

            // Act
            var result = builder.WithAllItems().Build();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.AllItems, Is.Not.Null);
        }

        [Test]
        public void EnumCacheBuilder_WithFilteredCache_ShouldBuildCache()
        {
            // Arrange
            var builder = new EnumCacheBuilder<TestEnum, TestAttribute>();

            // Act
            var result = builder
                .WithAllItems()
                .WithFilteredCache("test", attr => attr.Name == "Test1")
                .Build();

            // Assert
            Assert.That(result, Is.Not.Null);
            var filtered = result.GetFilteredCache("test");
            Assert.That(filtered, Is.Not.Null);
        }

        [Test]
        public void InterfaceCacheBuilder_WithDataExtractor_ShouldBuildCache()
        {
            // Arrange
            var serviceProvider = Substitute.For<IServiceProvider>();
            var builder = new InterfaceCacheBuilder<ITestInterface, string, string>(serviceProvider);

            // Act
            var result = builder
                .WithDataExtractor(instance => new Dictionary<string, string> { { "key1", "value1" } })
                .Build();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.AllItems, Is.Not.Null);
        }

        [Test]
        public void InterfaceCacheBuilder_WithFilteredCache_ShouldBuildCache()
        {
            // Arrange
            var serviceProvider = Substitute.For<IServiceProvider>();
            var builder = new InterfaceCacheBuilder<ITestInterface, string, string>(serviceProvider);

            // Act
            var result = builder
                .WithDataExtractor(instance => new Dictionary<string, string> { { "key1", "value1" }, { "key2", "value2" } })
                .WithFilteredCache("test", value => value == "value1")
                .Build();

            // Assert
            Assert.That(result, Is.Not.Null);
            var filtered = result.GetFilteredCache("test");
            Assert.That(filtered, Is.Not.Null);
        }

    }

    // Test types for the simplified tests
    public enum TestEnum
    {
        Value1,
        Value2
    }

    public class TestAttribute : Attribute
    {
        public string Name { get; set; } = "Test";
    }
}
