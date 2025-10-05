using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Service;
using SWLOR.Shared.Domain.World.Enums;

namespace SWLOR.Test.Shared.Caching.Service
{
    [TestFixture]
    public class EnumCacheBuilderTests : TestBase
    {
        private EnumCacheBuilder<PlanetType, PlanetAttribute> _builder;

        [SetUp]
        public void SetUp()
        {
            _builder = new EnumCacheBuilder<PlanetType, PlanetAttribute>();
        }

        [Test]
        public void WithAllItems_ShouldReturnBuilderInstance()
        {
            // Act
            var result = _builder.WithAllItems();

            // Assert
            Assert.That(result, Is.EqualTo(_builder));
        }

        [Test]
        public void WithAllItems_ShouldPopulateAllItems()
        {
            // Act
            var result = _builder.WithAllItems().Build();

            // Assert
            Assert.That(result.AllItems, Is.Not.Empty);
            // Note: This test depends on actual enum values with attributes in the system
        }

        [Test]
        public void WithFilteredCache_ShouldReturnBuilderInstance()
        {
            // Arrange
            _builder.WithAllItems();

            // Act
            var result = _builder.WithFilteredCache("Active", attr => attr.IsActive);

            // Assert
            Assert.That(result, Is.EqualTo(_builder));
        }

        [Test]
        public void WithFilteredCache_ShouldCreateFilteredCache()
        {
            // Arrange
            _builder.WithAllItems();

            // Act
            var result = _builder.WithFilteredCache("Active", attr => attr.IsActive).Build();

            // Assert
            var filteredCache = result.GetFilteredCache("Active");
            Assert.That(filteredCache, Is.Not.Null);
            // Note: This test depends on actual enum values with attributes in the system
        }

        [Test]
        public void WithGroupedCache_ShouldReturnBuilderInstance()
        {
            // Arrange
            _builder.WithAllItems();

            // Act
            var result = _builder.WithGroupedCache("ByActive", attr => attr.IsActive);

            // Assert
            Assert.That(result, Is.EqualTo(_builder));
        }

        [Test]
        public void WithGroupedCache_ShouldCreateGroupedCache()
        {
            // Arrange
            _builder.WithAllItems();

            // Act
            var result = _builder.WithGroupedCache("ByActive", attr => attr.IsActive).Build();

            // Assert
            var groupedCache = result.GetGroupedCache<bool>("ByActive");
            Assert.That(groupedCache, Is.Not.Null);
            // Note: This test depends on actual enum values with attributes in the system
        }

        [Test]
        public void WithFilteredGroupedCache_ShouldReturnBuilderInstance()
        {
            // Arrange
            _builder.WithAllItems();

            // Act
            var result = _builder.WithFilteredGroupedCache("ActiveByActive", attr => attr.IsActive, attr => attr.IsActive);

            // Assert
            Assert.That(result, Is.EqualTo(_builder));
        }

        [Test]
        public void WithFilteredGroupedCache_ShouldCreateFilteredGroupedCache()
        {
            // Arrange
            _builder.WithAllItems();

            // Act
            var result = _builder.WithFilteredGroupedCache("ActiveByActive", attr => attr.IsActive, attr => attr.IsActive).Build();

            // Assert
            var filteredGroupedCache = result.GetFilteredGroupedCache<bool>("ActiveByActive");
            Assert.That(filteredGroupedCache, Is.Not.Null);
            // Note: This test depends on actual enum values with attributes in the system
        }

        [Test]
        public void Build_ShouldReturnEnumCache()
        {
            // Arrange
            _builder.WithAllItems();

            // Act
            var result = _builder.Build();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<IEnumCache<PlanetType, PlanetAttribute>>());
        }

        [Test]
        public void MethodChaining_ShouldWorkCorrectly()
        {
            // Act
            var result = _builder
                .WithAllItems()
                .WithFilteredCache("Active", attr => attr.IsActive)
                .WithGroupedCache("ByActive", attr => attr.IsActive)
                .WithFilteredGroupedCache("ActiveByActive", attr => attr.IsActive, attr => attr.IsActive)
                .Build();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.AllItems, Is.Not.Null);
            Assert.That(result.GetFilteredCache("Active"), Is.Not.Null);
            Assert.That(result.GetGroupedCache<bool>("ByActive"), Is.Not.Null);
            Assert.That(result.GetFilteredGroupedCache<bool>("ActiveByActive"), Is.Not.Null);
        }
    }
}
