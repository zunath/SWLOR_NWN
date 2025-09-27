using NUnit.Framework;
using SWLOR.Shared.Abstractions;

namespace SWLOR.Test.Shared.Abstractions
{
    [TestFixture]
    public class EntityBaseTests
    {
        // Test entity class for testing EntityBase
        private class TestEntity : EntityBase
        {
            public string TestProperty { get; set; }
        }

        [Test]
        public void Constructor_ShouldInitializeProperties()
        {
            // Arrange & Act
            var entity = new TestEntity();

            // Assert
            Assert.That(entity.Id, Is.Not.Null.And.Not.Empty);
            Assert.That(entity.DateCreated, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(entity.EntityType, Is.EqualTo(nameof(TestEntity)));
        }

        [Test]
        public void Constructor_ShouldGenerateUniqueIds()
        {
            // Arrange & Act
            var entity1 = new TestEntity();
            var entity2 = new TestEntity();

            // Assert
            Assert.That(entity1.Id, Is.Not.EqualTo(entity2.Id));
        }

        [Test]
        public void Constructor_ShouldSetDateCreatedToCurrentTime()
        {
            // Arrange
            var beforeCreation = DateTime.UtcNow;

            // Act
            var entity = new TestEntity();
            var afterCreation = DateTime.UtcNow;

            // Assert
            Assert.That(entity.DateCreated, Is.GreaterThanOrEqualTo(beforeCreation));
            Assert.That(entity.DateCreated, Is.LessThanOrEqualTo(afterCreation));
        }

        [Test]
        public void Properties_ShouldBeSettable()
        {
            // Arrange
            var entity = new TestEntity();
            var testId = "test-id";
            var testDate = DateTime.UtcNow;
            var testType = "TestType";

            // Act
            entity.Id = testId;
            entity.DateCreated = testDate;
            entity.EntityType = testType;

            // Assert
            Assert.That(entity.Id, Is.EqualTo(testId));
            Assert.That(entity.DateCreated, Is.EqualTo(testDate));
            Assert.That(entity.EntityType, Is.EqualTo(testType));
        }

        [Test]
        public void EntityType_ShouldReflectActualTypeName()
        {
            // Arrange & Act
            var entity = new TestEntity();

            // Assert
            Assert.That(entity.EntityType, Is.EqualTo("TestEntity"));
        }
    }
}
