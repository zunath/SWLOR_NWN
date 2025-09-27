using NUnit.Framework;
using SWLOR.Shared.Abstractions;
using Newtonsoft.Json;

namespace SWLOR.Test.Shared.Abstractions
{
    [TestFixture]
    public class EntityBaseTests
    {
        // Test entity class for testing EntityBase
    private class TestEntity : EntityBase
    {
        public string? TestProperty { get; set; }
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

        [Test]
        public void JsonSerialization_ShouldSerializeCorrectly()
        {
            // Arrange
            var entity = new TestEntity();
            entity.TestProperty = "Test Value";

            // Act
            var json = JsonConvert.SerializeObject(entity);
            var deserializedEntity = JsonConvert.DeserializeObject<TestEntity>(json);

            // Assert
            Assert.That(deserializedEntity, Is.Not.Null);
            Assert.That(deserializedEntity.Id, Is.EqualTo(entity.Id));
            Assert.That(deserializedEntity.EntityType, Is.EqualTo(entity.EntityType));
            Assert.That(deserializedEntity.TestProperty, Is.EqualTo(entity.TestProperty));
            Assert.That(deserializedEntity.DateCreated, Is.EqualTo(entity.DateCreated).Within(TimeSpan.FromMilliseconds(1)));
        }

        [Test]
        public void JsonDeserialization_ShouldCreateValidEntity()
        {
            // Arrange
            var json = @"{
                ""Id"": ""test-id-123"",
                ""DateCreated"": ""2023-01-01T12:00:00Z"",
                ""EntityType"": ""TestEntity"",
                ""TestProperty"": ""Test Value""
            }";

            // Act
            var entity = JsonConvert.DeserializeObject<TestEntity>(json);

            // Assert
            Assert.That(entity, Is.Not.Null);
            Assert.That(entity.Id, Is.EqualTo("test-id-123"));
            Assert.That(entity.EntityType, Is.EqualTo("TestEntity"));
            Assert.That(entity.TestProperty, Is.EqualTo("Test Value"));
            Assert.That(entity.DateCreated, Is.EqualTo(new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc)));
        }

        [Test]
        public void JsonSerialization_ShouldHandleNullValues()
        {
            // Arrange
            var entity = new TestEntity();
            entity.TestProperty = null;

            // Act
            var json = JsonConvert.SerializeObject(entity);
            var deserializedEntity = JsonConvert.DeserializeObject<TestEntity>(json);

            // Assert
            Assert.That(deserializedEntity, Is.Not.Null);
            Assert.That(deserializedEntity.TestProperty, Is.Null);
        }

        [Test]
        public void JsonSerialization_ShouldPreserveAllProperties()
        {
            // Arrange
            var entity = new TestEntity();
            entity.TestProperty = "Test Value";
            var originalId = entity.Id;
            var originalDateCreated = entity.DateCreated;
            var originalEntityType = entity.EntityType;

            // Act
            var json = JsonConvert.SerializeObject(entity);
            var deserializedEntity = JsonConvert.DeserializeObject<TestEntity>(json);

            // Assert
            Assert.That(deserializedEntity.Id, Is.EqualTo(originalId));
            Assert.That(deserializedEntity.DateCreated, Is.EqualTo(originalDateCreated).Within(TimeSpan.FromMilliseconds(1)));
            Assert.That(deserializedEntity.EntityType, Is.EqualTo(originalEntityType));
            Assert.That(deserializedEntity.TestProperty, Is.EqualTo("Test Value"));
        }

        [Test]
        public void Constructor_ShouldHandleVeryLongIds()
        {
            // Arrange & Act
            var entity = new TestEntity();
            entity.Id = new string('A', 1000); // Very long ID

            // Assert
            Assert.That(entity.Id, Is.EqualTo(new string('A', 1000)));
        }

        [Test]
        public void Constructor_ShouldHandleSpecialCharactersInEntityType()
        {
            // Arrange & Act
            var entity = new TestEntity();
            entity.EntityType = "Test-Entity_Type.123";

            // Assert
            Assert.That(entity.EntityType, Is.EqualTo("Test-Entity_Type.123"));
        }

        [Test]
        public void Constructor_ShouldHandleUnicodeCharacters()
        {
            // Arrange & Act
            var entity = new TestEntity();
            entity.Id = "测试-123-🚀";
            entity.EntityType = "测试实体";

            // Assert
            Assert.That(entity.Id, Is.EqualTo("测试-123-🚀"));
            Assert.That(entity.EntityType, Is.EqualTo("测试实体"));
        }

        [Test]
        public void Constructor_ShouldHandleEmptyStrings()
        {
            // Arrange & Act
            var entity = new TestEntity();
            entity.Id = "";
            entity.EntityType = "";

            // Assert
            Assert.That(entity.Id, Is.EqualTo(""));
            Assert.That(entity.EntityType, Is.EqualTo(""));
        }

        [Test]
        public void Constructor_ShouldHandleMinMaxDateTimeValues()
        {
            // Arrange & Act
            var entity = new TestEntity();
            entity.DateCreated = DateTime.MinValue;

            // Assert
            Assert.That(entity.DateCreated, Is.EqualTo(DateTime.MinValue));

            // Act
            entity.DateCreated = DateTime.MaxValue;

            // Assert
            Assert.That(entity.DateCreated, Is.EqualTo(DateTime.MaxValue));
        }
    }
}
