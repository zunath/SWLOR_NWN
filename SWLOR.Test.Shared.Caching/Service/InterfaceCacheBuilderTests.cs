using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Service;

namespace SWLOR.Test.Shared.Caching.Service
{
    [TestFixture]
    public class InterfaceCacheBuilderTests : TestBase
    {
        private IServiceProvider _mockServiceProvider;
        private InterfaceCacheBuilder<ITestInterface, string, string> _builder;

        [SetUp]
        public void SetUp()
        {
            _mockServiceProvider = Substitute.For<IServiceProvider>();
            _builder = new InterfaceCacheBuilder<ITestInterface, string, string>(_mockServiceProvider);
        }

        [Test]
        public void Constructor_WithNullServiceProvider_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => new InterfaceCacheBuilder<ITestInterface, string, string>(null));
        }

        [Test]
        public void Constructor_WithServiceProvider_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => new InterfaceCacheBuilder<ITestInterface, string, string>(_mockServiceProvider));
        }

        [Test]
        public void WithDataExtractor_ShouldReturnBuilderInstance()
        {
            // Arrange
            var dataExtractor = new Func<ITestInterface, Dictionary<string, string>>(instance => new Dictionary<string, string>());

            // Act
            var result = _builder.WithDataExtractor(dataExtractor);

            // Assert
            Assert.That(result, Is.EqualTo(_builder));
        }

        [Test]
        public void WithDataExtractor_WithServiceProvider_ShouldUseServiceProvider()
        {
            // Arrange
            var mockInstance = Substitute.For<ITestInterface>();
            var testData = new Dictionary<string, string> { { "key1", "value1" } };
            var dataExtractor = new Func<ITestInterface, Dictionary<string, string>>(instance => testData);

            _mockServiceProvider.GetRequiredService(typeof(TestInterfaceImpl))
                .Returns(mockInstance);

            // Act
            var result = _builder.WithDataExtractor(dataExtractor).Build();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.AllItems, Is.Not.Empty);
            _mockServiceProvider.Received().GetRequiredService(typeof(TestInterfaceImpl));
        }

        [Test]
        public void WithDataExtractor_WithServiceProviderException_ShouldFallbackToActivator()
        {
            // Arrange
            var testData = new Dictionary<string, string> { { "key1", "value1" } };
            var dataExtractor = new Func<ITestInterface, Dictionary<string, string>>(instance => testData);

            _mockServiceProvider.GetRequiredService(typeof(TestInterfaceImpl))
                .Returns(x => throw new InvalidOperationException("Service not found"));

            // Act
            var result = _builder.WithDataExtractor(dataExtractor).Build();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.AllItems, Is.Not.Empty);
        }

        [Test]
        public void WithDataExtractor_WithNullServiceProvider_ShouldUseActivator()
        {
            // Arrange
            var builderWithNullProvider = new InterfaceCacheBuilder<ITestInterface, string, string>(null);
            var testData = new Dictionary<string, string> { { "key1", "value1" } };
            var dataExtractor = new Func<ITestInterface, Dictionary<string, string>>(instance => testData);

            // Act
            var result = builderWithNullProvider.WithDataExtractor(dataExtractor).Build();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.AllItems, Is.Not.Empty);
        }

        [Test]
        public void WithFilteredCache_ShouldReturnBuilderInstance()
        {
            // Arrange
            _builder.WithDataExtractor(instance => new Dictionary<string, string>());

            // Act
            var result = _builder.WithFilteredCache("Filtered", value => value.Length > 3);

            // Assert
            Assert.That(result, Is.EqualTo(_builder));
        }

        [Test]
        public void WithGroupedCache_ShouldReturnBuilderInstance()
        {
            // Arrange
            _builder.WithDataExtractor(instance => new Dictionary<string, string>());

            // Act
            var result = _builder.WithGroupedCache("Grouped", value => value.Length);

            // Assert
            Assert.That(result, Is.EqualTo(_builder));
        }

        [Test]
        public void WithFilteredGroupedCache_ShouldReturnBuilderInstance()
        {
            // Arrange
            _builder.WithDataExtractor(instance => new Dictionary<string, string>());

            // Act
            var result = _builder.WithFilteredGroupedCache("FilteredGrouped", value => value.Length > 3, value => value.Length);

            // Assert
            Assert.That(result, Is.EqualTo(_builder));
        }

        [Test]
        public void Build_ShouldReturnInterfaceCache()
        {
            // Arrange
            _builder.WithDataExtractor(instance => new Dictionary<string, string>());

            // Act
            var result = _builder.Build();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<IInterfaceCache<string, string>>());
        }

        [Test]
        public void MethodChaining_ShouldWorkCorrectly()
        {
            // Arrange
            var testData = new Dictionary<string, string> { { "key1", "value1" }, { "key2", "value2" } };
            var dataExtractor = new Func<ITestInterface, Dictionary<string, string>>(instance => testData);

            // Act
            var result = _builder
                .WithDataExtractor(dataExtractor)
                .WithFilteredCache("Filtered", value => value.Length > 3)
                .WithGroupedCache("Grouped", value => value.Length)
                .WithFilteredGroupedCache("FilteredGrouped", value => value.Length > 3, value => value.Length)
                .Build();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.AllItems, Is.Not.Null);
            Assert.That(result.GetFilteredCache("Filtered"), Is.Not.Null);
            Assert.That(result.GetGroupedCache<int>("Grouped"), Is.Not.Null);
            Assert.That(result.GetFilteredGroupedCache<int>("FilteredGrouped"), Is.Not.Null);
        }
    }

    // Test interface for interface cache testing
    public interface ITestInterface
    {
        string GetData();
    }

    // Test implementation for testing
    public class TestInterfaceImpl : ITestInterface
    {
        public string GetData() => "test";
    }
}
